using OrderService.Application.Common.Interfaces;
using OrderService.Application.DTOs;

namespace OrderService.Application.Queries.ListOrders;

public sealed record ListOrdersQuery() : ICachedQuery<IReadOnlyCollection<OrderDto>>
{
    public string CacheKey => "orders:list";
    public TimeSpan Expiration => TimeSpan.FromMinutes(1);
}
