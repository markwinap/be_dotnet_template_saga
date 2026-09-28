namespace OrderService.Domain.Entities;

public sealed class OrderWorkflowExecution
{
    private OrderWorkflowExecution() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string CurrentStep { get; private set; } = "Started";
    public string Status { get; private set; } = "InProgress";
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime LastUpdatedAtUtc { get; private set; }
    public string? ErrorReason { get; private set; }
    public int RetryCount { get; private set; }

    public static OrderWorkflowExecution Start(Guid orderId)
    {
        var now = DateTime.UtcNow;
        return new OrderWorkflowExecution
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CurrentStep = "Started",
            Status = "InProgress",
            StartedAtUtc = now,
            LastUpdatedAtUtc = now
        };
    }

    public void RecordStep(string stepName, string status, string? errorReason = null)
    {
        CurrentStep = stepName;
        Status = status;
        ErrorReason = errorReason;
        LastUpdatedAtUtc = DateTime.UtcNow;

        if (status is "Completed" or "Failed")
        {
            CompletedAtUtc = DateTime.UtcNow;
        }
    }

    public void IncrementRetry() => RetryCount++;
}
