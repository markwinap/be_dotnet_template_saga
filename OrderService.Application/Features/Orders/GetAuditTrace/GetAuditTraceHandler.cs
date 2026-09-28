using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Features.Orders.GetAuditTrace;

public static class GetAuditTraceHandler
{
    public static Task<IReadOnlyCollection<OrderStateTrace>> Handle(GetAuditTraceQuery query, IAuditRepository auditRepository, CancellationToken cancellationToken)
        => auditRepository.ListOrderTracesAsync(query.OrderId, cancellationToken);
}
