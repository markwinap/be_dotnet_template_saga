using System.Text.Json;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Messages;
using OrderService.Application.Messages.Saga;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;

namespace OrderService.Application.Features.Orders.SubmitOrder;

public static class SubmitOrderHandler
{
    public static async Task<object[]> Handle(
        SubmitOrderCommand command,
        IOrderRepository repository,
        IOrderSubmissionPolicy submissionPolicy,
        IAuditRepository auditRepository,
        IAuditContext auditContext,
        OrderSubmissionPolicyOptions policyOptions,
        TimeProvider timeProvider,
        ICacheService cacheService,
        CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(command.OrderId, command.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Order '{command.OrderId}' not found.");

        await submissionPolicy.ValidateAsync(order, command.CustomerId, cancellationToken);

        var previousStatus = order.Status.ToString();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        order.Submit(policyOptions, nowUtc);

        var trace = OrderStateTrace.Create(
            order.Id,
            previousStatus,
            order.Status.ToString(),
            auditContext.ActorId,
            null,
            auditContext.CorrelationId,
            JsonSerializer.Serialize(new { order.OrderNumber, order.CustomerId, Total = order.GetTotal() }));
        await auditRepository.AddAsync(trace, cancellationToken);

        await cacheService.RemoveAsync($"customer:{order.CustomerId}:order:{order.Id}", cancellationToken);
        await cacheService.RemoveAsync($"customer:{order.CustomerId}:orders:list", cancellationToken);

        return
        [
            new OrderSubmittedEvent(order.Id, order.OrderNumber),
            new OrderSubmitted(order.Id)
        ];
    }
}
