using MediatR;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.DTOs;

namespace OrderService.Application.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    string CustomerId,
    string ShippingLine1,
    string City,
    string State,
    string PostalCode,
    string Country,
    IReadOnlyCollection<CreateOrderItemRequest> Items) : IRequest<OrderDto>, IAuditableCommand
{
    public string Action => "CreateOrder";
    public string EntityType => "Order";
    public string EntityId => string.Empty;
}

public sealed record CreateOrderItemRequest(string ProductCode, int Quantity, decimal UnitPrice, string Currency);
