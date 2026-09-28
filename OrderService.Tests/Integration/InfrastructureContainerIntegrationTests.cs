using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Entities;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Caching;
using OrderService.Infrastructure.Data;

namespace OrderService.Tests.Integration;

[Collection("ContainerIntegration")]
public sealed class InfrastructureContainerIntegrationTests(IntegrationTestEnvironment environment)
{
    [Fact]
    public async Task EfCore_ShouldPersistAndLoadOrderAggregate()
    {
        if (!environment.IsDockerAvailable)
        {
            return;
        }

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(environment.SqlConnectionString)
            .Options;

        await using var dbContext = new OrderDbContext(options);
        await dbContext.Database.MigrateAsync();

        var order = Order.Create("integration-customer", new Address("1 Test Way", "Seattle", "WA", "98052", "US"), TimeSpan.FromHours(24));
        order.AddItem("SKU-INTEGRATION", 2, new Money(19.99m, "USD"));

        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var loadedOrder = await dbContext.Orders
            .Include(x => x.Items)
            .SingleAsync(x => x.Id == order.Id);

        loadedOrder.CustomerId.Should().Be("integration-customer");
        loadedOrder.Items.Should().HaveCount(1);
        loadedOrder.GetTotal().Should().Be(39.98m);
    }

    [Fact]
    public async Task Redis_ShouldCacheAndReturnPayload()
    {
        if (!environment.IsDockerAvailable)
        {
            return;
        }

        var services = new ServiceCollection();
        services.AddStackExchangeRedisCache(options => options.Configuration = environment.RedisConnectionString);
        services.AddScoped<RedisCacheService>();

        await using var provider = services.BuildServiceProvider();
        var cacheService = provider.GetRequiredService<RedisCacheService>();

        var key = $"it:{Guid.NewGuid():N}";
        var payload = new CachedModel("integration", 42);

        await cacheService.SetAsync(key, payload, TimeSpan.FromMinutes(1));
        var cached = await cacheService.GetAsync<CachedModel>(key);

        cached.Should().NotBeNull();
        cached!.Name.Should().Be("integration");
        cached.Value.Should().Be(42);
    }

    [Fact]
    public async Task Audit_ShouldPersistAuditLogAndOrderStateTrace()
    {
        if (!environment.IsDockerAvailable)
        {
            return;
        }

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(environment.SqlConnectionString)
            .Options;

        await using var dbContext = new OrderDbContext(options);
        await dbContext.Database.MigrateAsync();

        var auditLog = AuditLog.Create(
            "actor-1",
            "actor@example.com",
            "SubmitOrder",
            "Order",
            Guid.NewGuid().ToString("N"),
            null,
            "{\"status\":\"Submitted\"}",
            null,
            "OrderService.Api",
            Guid.NewGuid().ToString("N"),
            "127.0.0.1",
            "session-1");

        var orderId = Guid.NewGuid();
        var trace = OrderStateTrace.Create(
            orderId,
            "Draft",
            "Submitted",
            "actor-1",
            null,
            auditLog.CorrelationId,
            "{\"total\":100.00}");

        await dbContext.AuditLogs.AddAsync(auditLog);
        await dbContext.OrderStateTraces.AddAsync(trace);
        await dbContext.SaveChangesAsync();

        var loadedLog = await dbContext.AuditLogs.SingleAsync(x => x.EventId == auditLog.EventId);
        var loadedTrace = await dbContext.OrderStateTraces.SingleAsync(x => x.TraceId == trace.TraceId);

        loadedLog.Action.Should().Be("SubmitOrder");
        loadedLog.RowHash.Should().NotBeNullOrWhiteSpace();
        loadedTrace.OrderId.Should().Be(orderId);
        loadedTrace.State.Should().Be("Submitted");
    }

    private sealed record CachedModel(string Name, int Value);
}
