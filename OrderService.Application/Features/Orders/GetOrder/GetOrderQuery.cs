namespace OrderService.Application.Features.Orders.GetOrder;

public sealed record GetOrderQuery(Guid OrderId, string CustomerId);
