using MediatR;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.DTOs;

namespace OrderService.Application.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId, string CustomerId) : ICachedQuery<OrderDto?>
{
    public string CacheKey => $"customer:{CustomerId}:order:{OrderId}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
