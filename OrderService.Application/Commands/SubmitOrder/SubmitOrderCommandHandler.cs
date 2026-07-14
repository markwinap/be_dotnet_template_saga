using MediatR;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Commands.SubmitOrder;

public sealed class SubmitOrderCommandHandler(
    IOrderRepository repository,
    IOrderSagaOrchestrator sagaOrchestrator,
    IServiceBusPublisher serviceBusPublisher) : IRequestHandler<SubmitOrderCommand>
{
    public async Task Handle(SubmitOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' not found.");

        order.Submit();
        await repository.SaveChangesAsync(cancellationToken);

        await serviceBusPublisher.PublishAsync("orders.submitted", new { order.Id, order.OrderNumber }, cancellationToken);
        await sagaOrchestrator.HandleOrderSubmittedAsync(order.Id, cancellationToken);
    }
}
