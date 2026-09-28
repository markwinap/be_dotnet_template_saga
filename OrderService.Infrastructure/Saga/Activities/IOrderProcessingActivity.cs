namespace OrderService.Infrastructure.Saga.Activities;

public interface IOrderProcessingActivity
{
    string Name { get; }
    Task<ActivityResult> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public sealed record ActivityResult(bool Succeeded, string? Error = null, string? CorrelationId = null);
