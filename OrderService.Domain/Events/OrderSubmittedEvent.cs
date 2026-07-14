namespace OrderService.Domain.Events;

public sealed record OrderSubmittedEvent(Guid OrderId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
