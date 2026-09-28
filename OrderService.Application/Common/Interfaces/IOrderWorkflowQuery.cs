namespace OrderService.Application.Common.Interfaces;

public interface IOrderWorkflowQuery
{
    Task<OrderWorkflowState?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public sealed record OrderWorkflowState(
    Guid Id,
    string CurrentStep,
    string Status,
    DateTime StartedAtUtc,
    DateTime LastUpdatedAtUtc,
    DateTime? CompletedAtUtc,
    string? ErrorReason,
    int RetryCount);
