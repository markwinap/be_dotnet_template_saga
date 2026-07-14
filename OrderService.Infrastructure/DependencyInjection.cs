using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Caching;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Data.Repositories;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Saga;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConnection = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=OrderService;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";

        services.AddDbContext<OrderDbContext>(options => options.UseSqlServer(dbConnection));
        services.AddScoped<IOrderRepository, OrderRepository>();

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        services.AddScoped<ICacheService, RedisCacheService>();

        var serviceBusConnection = configuration.GetConnectionString("ServiceBus")
            ?? "Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dev";
        services.AddSingleton(_ => new ServiceBusClient(serviceBusConnection));
        services.AddScoped<IServiceBusPublisher, AzureServiceBusPublisher>();

        services.AddScoped<IOrderSagaOrchestrator, OrderProcessingSaga>();

        return services;
    }
}
