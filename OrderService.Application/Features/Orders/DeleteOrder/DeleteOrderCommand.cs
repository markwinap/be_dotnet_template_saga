using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Features.Orders.DeleteOrder;

public sealed record DeleteOrderCommand(Guid OrderId, string CustomerId) : IAuditableCommand
{
    public string Action => "DeleteOrder";
    public string EntityType => "Order";
    public string EntityId => OrderId.ToString("N");
}
