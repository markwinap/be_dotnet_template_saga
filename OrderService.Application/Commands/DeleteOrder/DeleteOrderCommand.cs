using MediatR;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Commands.DeleteOrder;

public sealed record DeleteOrderCommand(Guid OrderId, string CustomerId) : IRequest, IAuditableCommand
{
    public string Action => "DeleteOrder";
    public string EntityType => "Order";
    public string EntityId => OrderId.ToString("N");
}
