using Wolverine.Persistence.Sagas;

namespace OrderService.Application.Messages.Saga;

public sealed record ReserveInventory(Guid OrderId);
public sealed record InventoryReserved([property: SagaIdentity] Guid OrderId);
public sealed record InventoryReservationFailed([property: SagaIdentity] Guid OrderId, string Reason);

public sealed record ProcessPayment(Guid OrderId);
public sealed record PaymentProcessed([property: SagaIdentity] Guid OrderId);
public sealed record PaymentFailed([property: SagaIdentity] Guid OrderId, string Reason);

public sealed record CreateShipment(Guid OrderId);
public sealed record ShipmentCreated([property: SagaIdentity] Guid OrderId);
public sealed record ShipmentFailed([property: SagaIdentity] Guid OrderId, string Reason);
