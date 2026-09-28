using Wolverine.Persistence.Sagas;

namespace OrderService.Application.Messages.Saga;

public sealed record OrderSubmitted([property: SagaIdentity] Guid OrderId);
