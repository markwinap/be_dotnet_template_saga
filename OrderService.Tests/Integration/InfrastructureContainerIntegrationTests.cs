using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Caching;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Messaging;

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
        await dbContext.Database.EnsureCreatedAsync();

        var order = Order.Create("integration-customer", new Address("1 Test Way", "Seattle", "WA", "98052", "US"));
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
    public async Task ServiceBus_ShouldPublishAndReceiveMessage()
    {
        if (!environment.IsDockerAvailable)
        {
            return;
        }

        await using var client = new ServiceBusClient(environment.ServiceBusConnectionString);
        var publisher = new AzureServiceBusPublisher(client, NullLogger<AzureServiceBusPublisher>.Instance);

        var marker = Guid.NewGuid().ToString("N");
        await publisher.PublishAsync("integration-topic", new { marker, createdAt = DateTime.UtcNow });

        await using var receiver = client.CreateReceiver("integration-topic", "integration-subscription");
        var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(20));

        message.Should().NotBeNull();
        message!.Body.ToString().Should().Contain(marker);

        await receiver.CompleteMessageAsync(message);
    }

    private sealed record CachedModel(string Name, int Value);
}
