using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

public sealed class OrderItem
{
    private OrderItem() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string ProductCode { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();

    public static OrderItem Create(Guid orderId, string productCode, int quantity, Money unitPrice)
    {
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductCode = productCode,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    public decimal GetLineTotal() => Quantity * UnitPrice.Amount;
}
