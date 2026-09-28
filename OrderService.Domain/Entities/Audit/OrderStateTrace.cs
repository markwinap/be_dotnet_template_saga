namespace OrderService.Domain.Entities.Audit;

public sealed class OrderStateTrace
{
    private OrderStateTrace() { }

    public Guid TraceId { get; private set; }
    public Guid OrderId { get; private set; }
    public string? PreviousState { get; private set; }
    public string State { get; private set; } = string.Empty;
    public DateTime ChangedAtUtc { get; private set; }
    public string ChangedById { get; private set; } = string.Empty;
    public string? ChangeReason { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? PayloadJson { get; private set; }

    public static OrderStateTrace Create(
        Guid orderId,
        string? previousState,
        string state,
        string changedById,
        string? changeReason,
        string? correlationId,
        string? payloadJson)
    {
        return new OrderStateTrace
        {
            TraceId = Guid.NewGuid(),
            OrderId = orderId,
            PreviousState = previousState,
            State = state,
            ChangedAtUtc = DateTime.UtcNow,
            ChangedById = changedById,
            ChangeReason = changeReason,
            CorrelationId = correlationId,
            PayloadJson = payloadJson
        };
    }
}
