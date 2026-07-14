using MediatR;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.DTOs;

namespace OrderService.Application.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : ICachedQuery<OrderDto?>
{
    public string CacheKey => $"order:{OrderId}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
