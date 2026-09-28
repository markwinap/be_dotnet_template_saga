using System.Text.Json;
using MediatR;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Commands.DeleteOrder;

public sealed class DeleteOrderCommandHandler(
    IOrderRepository repository,
    ICacheService cacheService,
    IAuditRepository auditRepository,
    IAuditContext auditContext) : IRequestHandler<DeleteOrderCommand>
{
    public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, request.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' not found.");

        var previousStatus = order.Status.ToString();
        var orderNumber = order.OrderNumber;
        var customerId = order.CustomerId;

        repository.Remove(order);
        await repository.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveAsync($"customer:{order.CustomerId}:order:{order.Id}", cancellationToken);
        await cacheService.RemoveAsync($"customer:{order.CustomerId}:orders:list", cancellationToken);

        var trace = OrderStateTrace.Create(
            order.Id,
            previousStatus,
            "Deleted",
            auditContext.ActorId,
            null,
            auditContext.CorrelationId,
            JsonSerializer.Serialize(new { OrderNumber = orderNumber, CustomerId = customerId }));
        await auditRepository.AddAsync(trace, cancellationToken);
        await auditRepository.SaveChangesAsync(cancellationToken);
    }
}
