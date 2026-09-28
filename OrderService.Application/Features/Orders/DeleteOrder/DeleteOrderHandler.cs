using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Features.Orders.DeleteOrder;

public static class DeleteOrderHandler
{
    public static async Task Handle(
        DeleteOrderCommand command,
        IOrderRepository repository,
        ICacheService cacheService,
        CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(command.OrderId, command.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Order '{command.OrderId}' not found.");

        repository.Remove(order);
        await cacheService.RemoveAsync($"customer:{command.CustomerId}:order:{command.OrderId}", cancellationToken);
        await cacheService.RemoveAsync($"customer:{command.CustomerId}:orders:list", cancellationToken);
    }
}
