using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Sagas;

namespace OrderService.Infrastructure.Data.Repositories;

public sealed class OrderWorkflowQuery(OrderDbContext dbContext) : IOrderWorkflowQuery
{
    public async Task<OrderWorkflowState?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var saga = await dbContext.OrderWorkflowExecutions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        return saga is null
            ? null
            : new OrderWorkflowState(
                saga.Id,
                saga.CurrentStep,
                saga.Status,
                saga.StartedAtUtc,
                saga.LastUpdatedAtUtc,
                saga.CompletedAtUtc,
                saga.ErrorReason,
                saga.RetryCount);
    }
}
