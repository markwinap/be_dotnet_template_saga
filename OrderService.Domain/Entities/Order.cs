using OrderService.Domain.Enums;
using OrderService.Domain.Events;
using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private Order() { }

    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public Address ShippingAddress { get; private set; } = new("", "", "", "", "");
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public DateTime CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Order Create(string customerId, Address shippingAddress)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            CustomerId = customerId,
            ShippingAddress = shippingAddress,
            Status = OrderStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };

        order.Raise(new OrderCreatedEvent(order.Id, order.OrderNumber));
        return order;
    }

    public void AddItem(string productCode, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainException("Only draft orders can be modified.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        _items.Add(OrderItem.Create(Id, productCode, quantity, unitPrice));
    }

    public void Submit()
    {
        if (!_items.Any())
        {
            throw new DomainException("Cannot submit an order without items.");
        }

        if (Status != OrderStatus.Draft)
        {
            throw new DomainException("Only draft orders can be submitted.");
        }

        Status = OrderStatus.Submitted;
        Raise(new OrderSubmittedEvent(Id));
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
        {
            return;
        }

        Status = OrderStatus.Cancelled;
    }

    public decimal GetTotal() => _items.Sum(x => x.GetLineTotal());

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
