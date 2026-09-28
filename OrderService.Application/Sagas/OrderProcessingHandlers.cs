using Microsoft.Extensions.Logging;
using OrderService.Application.Messages.Saga;

namespace OrderService.Application.Sagas;

public static class OrderProcessingHandlers
{
    public static InventoryReserved Handle(ReserveInventory message, ILogger<ReserveInventory> logger)
    {
        logger.LogInformation("Reserving inventory for order {OrderId}", message.OrderId);
        return new InventoryReserved(message.OrderId);
    }

    public static PaymentProcessed Handle(ProcessPayment message, ILogger<ProcessPayment> logger)
    {
        logger.LogInformation("Processing payment for order {OrderId}", message.OrderId);
        return new PaymentProcessed(message.OrderId);
    }

    public static ShipmentCreated Handle(CreateShipment message, ILogger<CreateShipment> logger)
    {
        logger.LogInformation("Creating shipment for order {OrderId}", message.OrderId);
        return new ShipmentCreated(message.OrderId);
    }
}
