using MediatR;

namespace OrderService.Application.Commands.SubmitOrder;

public sealed record SubmitOrderCommand(Guid OrderId) : IRequest;
