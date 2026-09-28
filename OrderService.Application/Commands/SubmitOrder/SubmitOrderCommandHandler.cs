using System.Text.Json;
using MediatR;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Events;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;

namespace OrderService.Application.Commands.SubmitOrder;

public sealed class SubmitOrderCommandHandler(
    IOrderRepository repository,
    IOrderSagaOrchestrator sagaOrchestrator,
    IOutboxStore outboxStore,
    ICacheService cacheService,
    IOrderSubmissionPolicy submissionPolicy,
    IAuditRepository auditRepository,
    IAuditContext auditContext,
    OrderSubmissionPolicyOptions policyOptions,
    TimeProvider timeProvider) : IRequestHandler<SubmitOrderCommand>
{
    public async Task Handle(SubmitOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, request.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' not found.");

        await submissionPolicy.ValidateAsync(order, request.CustomerId, cancellationToken);

        var previousStatus = order.Status.ToString();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        order.Submit(policyOptions, nowUtc);
        await repository.SaveChangesAsync(cancellationToken);

        var domainEvent = order.DomainEvents.OfType<OrderSubmittedEvent>().Single();
        var outboxMessage = OutboxMessage.Create(
            domainEvent.GetType().FullName!,
            "orders.submitted",
            JsonSerializer.Serialize(new { domainEvent.OrderId, order.OrderNumber }),
            order.Id.ToString("N"));
        await outboxStore.AddAsync(outboxMessage, cancellationToken);
        await outboxStore.SaveChangesAsync(cancellationToken);

        order.ClearDomainEvents();

        var trace = OrderStateTrace.Create(
            order.Id,
            previousStatus,
            order.Status.ToString(),
            auditContext.ActorId,
            null,
            auditContext.CorrelationId,
            JsonSerializer.Serialize(new { order.OrderNumber, order.CustomerId, Total = order.GetTotal() }));
        await auditRepository.AddAsync(trace, cancellationToken);
        await auditRepository.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync($"customer:{order.CustomerId}:order:{order.Id}", cancellationToken);
        await cacheService.RemoveAsync($"customer:{order.CustomerId}:orders:list", cancellationToken);

        await sagaOrchestrator.HandleOrderSubmittedAsync(order.Id, cancellationToken);
    }
}
