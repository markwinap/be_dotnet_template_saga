namespace OrderService.Domain.Events;

public sealed record OrderCreatedEvent(Guid OrderId, string OrderNumber) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
