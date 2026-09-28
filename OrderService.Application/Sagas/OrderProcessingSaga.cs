using OrderService.Application.Messages.Saga;
using Wolverine;

namespace OrderService.Application.Sagas;

public sealed class OrderProcessingSaga : Saga
{
    public Guid Id { get; set; }
    public string CurrentStep { get; set; } = "Started";
    public string Status { get; set; } = "InProgress";
    public DateTime StartedAtUtc { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorReason { get; set; }
    public int RetryCount { get; set; }

    public static (OrderProcessingSaga, OutgoingMessages) Start(OrderSubmitted message)
    {
        var saga = new OrderProcessingSaga
        {
            Id = message.OrderId,
            CurrentStep = "ReservingInventory",
            Status = "InProgress",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        var messages = new OutgoingMessages();
        messages.Add(new ReserveInventory(message.OrderId));
        return (saga, messages);
    }

    public ProcessPayment? Handle(InventoryReserved message)
    {
        CurrentStep = "ProcessingPayment";
        LastUpdatedAtUtc = DateTime.UtcNow;
        return new ProcessPayment(message.OrderId);
    }

    public void Handle(InventoryReservationFailed message)
    {
        CurrentStep = "InventoryReservationFailed";
        Status = "Failed";
        ErrorReason = message.Reason;
        CompletedAtUtc = DateTime.UtcNow;
        MarkCompleted();
    }

    public CreateShipment? Handle(PaymentProcessed message)
    {
        CurrentStep = "CreatingShipment";
        LastUpdatedAtUtc = DateTime.UtcNow;
        return new CreateShipment(message.OrderId);
    }

    public void Handle(PaymentFailed message)
    {
        CurrentStep = "PaymentFailed";
        Status = "Failed";
        ErrorReason = message.Reason;
        CompletedAtUtc = DateTime.UtcNow;
        MarkCompleted();
    }

    public void Handle(ShipmentCreated message)
    {
        CurrentStep = "Completed";
        Status = "Completed";
        CompletedAtUtc = DateTime.UtcNow;
        MarkCompleted();
    }

    public void Handle(ShipmentFailed message)
    {
        CurrentStep = "ShipmentFailed";
        Status = "Failed";
        ErrorReason = message.Reason;
        CompletedAtUtc = DateTime.UtcNow;
        MarkCompleted();
    }
}
