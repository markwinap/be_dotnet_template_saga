namespace OrderService.Application.Common.Interfaces;

public interface IOrderSagaOrchestrator
{
    Task HandleOrderSubmittedAsync(Guid orderId, CancellationToken cancellationToken = default);
}
