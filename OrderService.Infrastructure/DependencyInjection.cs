using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Caching;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Data.Repositories;
using Wolverine.EntityFrameworkCore;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConnection = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=OrderService;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";

        services.AddDbContextWithWolverineIntegration<OrderDbContext>(options => options.UseSqlServer(dbConnection));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerOrderQuery, CustomerOrderQuery>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IOrderWorkflowQuery, OrderWorkflowQuery>();

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}
