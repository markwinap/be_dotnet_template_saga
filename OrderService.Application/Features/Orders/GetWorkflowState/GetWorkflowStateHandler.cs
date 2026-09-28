using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Features.Orders.GetWorkflowState;

public static class GetWorkflowStateHandler
{
    public static Task<OrderWorkflowState?> Handle(GetWorkflowStateQuery query, IOrderWorkflowQuery workflowQuery, CancellationToken cancellationToken)
        => workflowQuery.GetByOrderIdAsync(query.OrderId, cancellationToken);
}
