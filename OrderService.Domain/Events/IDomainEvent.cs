namespace OrderService.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
