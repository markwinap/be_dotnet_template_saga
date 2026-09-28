using FluentAssertions;
using Moq;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests;

public class OrderSubmissionPolicyTests
{
    private static readonly OrderSubmissionPolicyOptions DefaultOptions = new(
        MinimumOrderTotal: 10m,
        MaximumItemQuantity: 100,
        MaxSubmissionsPerMinute: 1,
        MaxSubmissionsPerDay: 100,
        DraftExpiration: TimeSpan.FromHours(24),
        BusinessHours: new BusinessHours(new TimeOnly(8, 0), new TimeOnly(20, 0)),
        TimeZone: TimeZoneInfo.Utc);

    [Fact]
    public async Task ValidateAsync_ShouldPass_WhenAllRulesAreSatisfied()
    {
        var order = CreateDraftOrder();
        var time = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var policy = CreatePolicy(DefaultOptions, 0, 0, CreateTimeProvider(time));

        var act = async () => await policy.ValidateAsync(order, "customer-1");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrow_WhenSubmissionRateLimitExceeded()
    {
        var order = CreateDraftOrder();
        var time = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var policy = CreatePolicy(DefaultOptions, submissionsInLastMinute: 1, submissionsToday: 0, CreateTimeProvider(time));

        var act = async () => await policy.ValidateAsync(order, "customer-1");

        await act.Should().ThrowAsync<DomainException>().WithMessage("*one order per minute*");
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrow_WhenDailySubmissionLimitExceeded()
    {
        var order = CreateDraftOrder();
        var time = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var options = DefaultOptions with { MaxSubmissionsPerDay = 2 };
        var policy = CreatePolicy(options, submissionsInLastMinute: 0, submissionsToday: 2, CreateTimeProvider(time));

        var act = async () => await policy.ValidateAsync(order, "customer-1");

        await act.Should().ThrowAsync<DomainException>().WithMessage("*daily limit*");
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrow_WhenOutsideBusinessHours()
    {
        var order = CreateDraftOrder();
        var mockTime = CreateTimeProvider(new DateTimeOffset(2026, 1, 1, 22, 0, 0, TimeSpan.Zero));
        var policy = CreatePolicy(DefaultOptions, 0, 0, mockTime);

        var act = async () => await policy.ValidateAsync(order, "customer-1");

        await act.Should().ThrowAsync<DomainException>().WithMessage("*business hours*");
    }

    private static OrderSubmissionPolicy CreatePolicy(
        OrderSubmissionPolicyOptions options,
        int submissionsInLastMinute,
        int submissionsToday,
        TimeProvider? timeProvider = null)
    {
        var customerOrderQuery = new Mock<ICustomerOrderQuery>();
        customerOrderQuery
            .Setup(x => x.GetSubmissionCountInLastMinuteAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissionsInLastMinute);
        customerOrderQuery
            .Setup(x => x.GetSubmissionCountTodayAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissionsToday);

        return new OrderSubmissionPolicy(options, customerOrderQuery.Object, timeProvider ?? TimeProvider.System);
    }

    private static Order CreateDraftOrder()
    {
        var order = Order.Create("customer-1", new Address("1 Main St", "Seattle", "WA", "98052", "US"), TimeSpan.FromHours(24));
        order.AddItem("SKU-1", 2, new Money(25m, "USD"));
        return order;
    }

    private static TimeProvider CreateTimeProvider(DateTimeOffset now)
    {
        var mock = new Mock<TimeProvider>();
        mock.Setup(x => x.GetUtcNow()).Returns(now);
        return mock.Object;
    }
}
