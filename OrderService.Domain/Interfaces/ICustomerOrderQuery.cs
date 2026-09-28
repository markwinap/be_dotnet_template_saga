namespace OrderService.Domain.Interfaces;

public interface ICustomerOrderQuery
{
    Task<int> GetSubmissionCountInLastMinuteAsync(string customerId, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<int> GetSubmissionCountTodayAsync(string customerId, DateTime nowUtc, CancellationToken cancellationToken = default);
}
