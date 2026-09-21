using MediatR;

namespace OrderService.Application.Commands.DeleteOrder;

public sealed record DeleteOrderCommand(Guid OrderId, string CustomerId) : IRequest;
