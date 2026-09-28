using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Features.Orders.SubmitOrder;

public sealed record SubmitOrderCommand(Guid OrderId, string CustomerId) : IAuditableCommand
{
    public string Action => "SubmitOrder";
    public string EntityType => "Order";
    public string EntityId => OrderId.ToString("N");
}
