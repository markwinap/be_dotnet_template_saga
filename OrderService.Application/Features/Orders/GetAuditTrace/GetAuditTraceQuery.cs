using OrderService.Domain.Entities.Audit;

namespace OrderService.Application.Features.Orders.GetAuditTrace;

public sealed record GetAuditTraceQuery(Guid OrderId);
