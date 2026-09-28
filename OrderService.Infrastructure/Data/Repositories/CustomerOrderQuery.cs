using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Infrastructure.Data.Repositories;

public sealed class CustomerOrderQuery(OrderDbContext dbContext) : ICustomerOrderQuery
{
    public async Task<int> GetSubmissionCountInLastMinuteAsync(string customerId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var oneMinuteAgo = nowUtc.AddMinutes(-1);
        return await dbContext.Orders
            .AsNoTracking()
            .CountAsync(
                o => o.CustomerId == customerId
                     && o.Status == OrderStatus.Submitted
                     && o.SubmittedAtUtc >= oneMinuteAgo
                     && o.SubmittedAtUtc <= nowUtc,
                cancellationToken);
    }

    public async Task<int> GetSubmissionCountTodayAsync(string customerId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var startOfDay = nowUtc.Date;
        var endOfDay = startOfDay.AddDays(1);
        return await dbContext.Orders
            .AsNoTracking()
            .CountAsync(
                o => o.CustomerId == customerId
                     && o.Status == OrderStatus.Submitted
                     && o.SubmittedAtUtc >= startOfDay
                     && o.SubmittedAtUtc < endOfDay,
                cancellationToken);
    }
}
