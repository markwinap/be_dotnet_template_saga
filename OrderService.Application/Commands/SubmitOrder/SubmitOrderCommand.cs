using MediatR;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Commands.SubmitOrder;

public sealed record SubmitOrderCommand(Guid OrderId, string CustomerId) : IRequest, IAuditableCommand
{
    public string Action => "SubmitOrder";
    public string EntityType => "Order";
    public string EntityId => OrderId.ToString("N");
}
