using OrderService.Application.Common.Interfaces;
using OrderService.Application.DTOs;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Features.Orders.ListOrders;

public static class ListOrdersHandler
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public static async Task<IReadOnlyCollection<OrderDto>> Handle(
        ListOrdersQuery query,
        IOrderRepository repository,
        ICacheService cacheService,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"customer:{query.CustomerId}:orders:list";
        var cached = await cacheService.GetAsync<IReadOnlyCollection<OrderDto>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var orders = await repository.ListAsync(query.CustomerId, cancellationToken);
        var result = orders
            .Select(order => new OrderDto(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.Status.ToString(),
                order.CreatedAtUtc,
                order.GetTotal(),
                order.Items.Select(x => new OrderItemDto(x.ProductCode, x.Quantity, x.UnitPrice.Amount, x.UnitPrice.Currency)).ToArray()))
            .ToArray();

        await cacheService.SetAsync(cacheKey, result, CacheExpiration, cancellationToken);
        return result;
    }
}
