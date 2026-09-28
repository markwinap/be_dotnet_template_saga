using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Features.Orders.CreateOrder;

public sealed record CreateOrderCommand(
    string CustomerId,
    string ShippingLine1,
    string City,
    string State,
    string PostalCode,
    string Country,
    IReadOnlyCollection<CreateOrderItemRequest> Items) : IAuditableCommand
{
    public string Action => "CreateOrder";
    public string EntityType => "Order";
    public string EntityId => string.Empty;
}

public sealed record CreateOrderItemRequest(
    string ProductCode,
    int Quantity,
    decimal UnitPrice,
    string Currency);
