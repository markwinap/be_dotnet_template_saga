using MediatR;
using OrderService.Application.Common.Exceptions;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Commands.DeleteOrder;

public sealed class DeleteOrderCommandHandler(IOrderRepository repository) : IRequestHandler<DeleteOrderCommand>
{
    public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' not found.");

        repository.Remove(order);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
