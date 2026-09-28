using Microsoft.Extensions.Logging;
using OrderService.Application.Messages.Saga;

namespace OrderService.Application.Sagas;

public static class OrderProcessingHandlers
{
    public static InventoryReserved Handle(ReserveInventory message)
    {
        return new InventoryReserved(message.OrderId);
    }

    public static PaymentProcessed Handle(ProcessPayment message)
    {
        return new PaymentProcessed(message.OrderId);
    }

    public static ShipmentCreated Handle(CreateShipment message)
    {
        return new ShipmentCreated(message.OrderId);
    }
}
