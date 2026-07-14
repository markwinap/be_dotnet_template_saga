using MediatR;
using OrderService.Application.DTOs;

namespace OrderService.Application.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    string CustomerId,
    string ShippingLine1,
    string City,
    string State,
    string PostalCode,
    string Country,
    IReadOnlyCollection<CreateOrderItemRequest> Items) : IRequest<OrderDto>;

public sealed record CreateOrderItemRequest(string ProductCode, int Quantity, decimal UnitPrice, string Currency);
