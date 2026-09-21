using OrderService.Application.Common.Interfaces;
using OrderService.Application.DTOs;

namespace OrderService.Application.Queries.ListOrders;

public sealed record ListOrdersQuery(string CustomerId) : ICachedQuery<IReadOnlyCollection<OrderDto>>
{
    public string CacheKey => $"customer:{CustomerId}:orders:list";
    public TimeSpan Expiration => TimeSpan.FromMinutes(1);
}
