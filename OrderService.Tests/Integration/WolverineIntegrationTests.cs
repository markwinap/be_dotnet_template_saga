using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alba;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrderService.Application.Features.Orders.CreateOrder;
using OrderService.Application.Features.Orders.SubmitOrder;
using OrderService.Infrastructure.Data;
using Wolverine;
using Wolverine.Tracking;

namespace OrderService.Tests.Integration;

[Collection("ContainerIntegration")]
public sealed class WolverineIntegrationTests(IntegrationTestEnvironment environment) : IAsyncLifetime
{
    private IAlbaHost? _host;

    public async Task InitializeAsync()
    {
        if (!environment.IsDockerAvailable)
        {
            return;
        }

        var fixedTime = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

        var configValues = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = environment.SqlConnectionString,
            ["ConnectionStrings:Redis"] = environment.RedisConnectionString,
            ["ConnectionStrings:ServiceBus"] = environment.ServiceBusConnectionString,
            ["Jwt:Secret"] = "super-secret-32-characters-long-key",
            ["Jwt:Issuer"] = "OrderService",
            ["Jwt:Audience"] = "OrderServiceClients",
            ["Jwt:ExpiresMinutes"] = "60",
            ["Wolverine:UseAzureServiceBus"] = "false"
        };

        _host = await AlbaHost.For<global::Program>(
            x =>
            {
                x.ConfigureServices(services =>
                {
                    services.AddSingleton<TimeProvider>(new FakeTimeProvider(fixedTime));
                });
            },
            ConfigurationOverride.Create(configValues));
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }
    }

    [Fact]
    public async Task Post_CreateOrder_ShouldPersistOrderAndReturn201()
    {
        if (_host is null)
        {
            return;
        }

        var customerId = $"create-{Guid.NewGuid():N}";
        var token = GenerateToken(customerId);
        var request = new CreateOrderCommand(
            customerId,
            "1 Main St",
            "Seattle",
            "WA",
            "98052",
            "US",
            [new CreateOrderItemRequest("SKU-IT", 2, 25m, "USD")]);

        await _host.Scenario(s =>
        {
            s.Post.Json(request, JsonStyle.MinimalApi).ToUrl("/api/orders");
            s.WithRequestHeader("Authorization", $"Bearer {token}");
            s.StatusCodeShouldBe(201);
        });

        await using var dbContext = CreateDbContext();
        var order = await dbContext.Orders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.CustomerId == customerId);
        order.Should().NotBeNull();
        order!.GetTotal().Should().Be(50m);
    }

    [Fact]
    public async Task Post_SubmitOrder_ShouldTransitionOrderToSubmittedAndReturn202()
    {
        if (_host is null)
        {
            return;
        }

        var customerId = $"saga-{Guid.NewGuid():N}";
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var token = GenerateToken(customerId);

        var order = await bus.InvokeAsync<OrderService.Application.DTOs.OrderDto>(
            new CreateOrderCommand(
                customerId,
                "1 Main St",
                "Seattle",
                "WA",
                "98052",
                "US",
                [new CreateOrderItemRequest("SKU-IT", 2, 10m, "USD")]));

        Func<IMessageContext, Task> submitAction = async _ =>
        {
            await _host.Scenario(s =>
            {
                s.Post.Json(new { }).ToUrl($"/api/orders/{order.Id}/submit");
                s.WithRequestHeader("Authorization", $"Bearer {token}");
                s.StatusCodeShouldBe(202);
            });
        };

        await _host.Services.TrackActivity().ExecuteAndWaitAsync(submitAction);

        await using var dbContext = CreateDbContext();
        var submittedOrder = await dbContext.Orders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == order.Id);
        submittedOrder.Should().NotBeNull();
        submittedOrder!.Status.Should().Be(OrderService.Domain.Enums.OrderStatus.Submitted);
    }

    [Fact]
    public async Task Send_OrderSubmitted_ShouldStartSaga()
    {
        if (_host is null)
        {
            return;
        }

        var orderId = Guid.NewGuid();

        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.InvokeAsync(new OrderService.Application.Messages.Saga.OrderSubmitted(orderId));

        await using var dbContext = CreateDbContext();
        var saga = await dbContext.OrderWorkflowExecutions.SingleOrDefaultAsync(x => x.Id == orderId);
        saga.Should().NotBeNull();
        saga!.Status.Should().Be("InProgress");
        saga.CurrentStep.Should().Be("ReservingInventory");
    }

    [Fact]
    public async Task Send_CreateOrderCommand_ShouldPersistOrderViaWolverinePipeline()
    {
        if (_host is null)
        {
            return;
        }

        var customerId = $"bus-{Guid.NewGuid():N}";
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var command = new CreateOrderCommand(
            customerId,
            "1 Main St",
            "Seattle",
            "WA",
            "98052",
            "US",
            [new CreateOrderItemRequest("SKU-BUS", 1, 15m, "USD")]);

        var result = await bus.InvokeAsync<OrderService.Application.DTOs.OrderDto>(command);

        result.Should().NotBeNull();
        result.CustomerId.Should().Be(customerId);

        await using var dbContext = CreateDbContext();
        var order = await dbContext.Orders.SingleOrDefaultAsync(o => o.Id == result.Id);
        order.Should().NotBeNull();
    }

    private static string GenerateToken(string subject)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-32-characters-long-key"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "OrderService",
            audience: "OrderServiceClients",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, subject), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(environment.SqlConnectionString)
            .Options;
        return new OrderDbContext(options);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
