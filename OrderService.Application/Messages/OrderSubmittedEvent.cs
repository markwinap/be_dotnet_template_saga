namespace OrderService.Application.Messages;

public sealed record OrderSubmittedEvent(Guid OrderId, string OrderNumber);
