using MediatR;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Commands.DeleteOrder;

public sealed class DeleteOrderCommandHandler(IOrderRepository repository, ICacheService cacheService) : IRequestHandler<DeleteOrderCommand>
{
    public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, request.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' not found.");

        repository.Remove(order);
        await repository.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveAsync($"customer:{order.CustomerId}:order:{order.Id}", cancellationToken);
        await cacheService.RemoveAsync($"customer:{order.CustomerId}:orders:list", cancellationToken);
    }
}
