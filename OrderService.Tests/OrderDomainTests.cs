using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;
using OrderService.Domain.Services;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests;

public class OrderDomainTests
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
    public void Submit_ShouldTransitionToSubmitted_WhenOrderIsValid()
    {
        var order = CreateDraftOrder();

        order.Submit(DefaultOptions, DateTime.UtcNow);

        order.Status.Should().Be(OrderStatus.Submitted);
        order.SubmittedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Submit_ShouldThrow_WhenOrderHasNoItems()
    {
        var order = Order.Create("customer-1", new Address("1 Main St", "Seattle", "WA", "98052", "US"), TimeSpan.FromHours(24));

        Action act = () => order.Submit(DefaultOptions, DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("Cannot submit an order without items.");
    }

    [Fact]
    public void Submit_ShouldThrow_WhenOrderTotalIsBelowMinimum()
    {
        var order = Order.Create("customer-1", new Address("1 Main St", "Seattle", "WA", "98052", "US"), TimeSpan.FromHours(24));
        order.AddItem("SKU-1", 1, new Money(1m, "USD"));

        Action act = () => order.Submit(DefaultOptions, DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*at least*");
    }

    [Fact]
    public void Submit_ShouldThrow_WhenItemQuantityExceedsMaximum()
    {
        var order = Order.Create("customer-1", new Address("1 Main St", "Seattle", "WA", "98052", "US"), TimeSpan.FromHours(24));
        order.AddItem("SKU-1", 101, new Money(1m, "USD"));

        Action act = () => order.Submit(DefaultOptions, DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*quantity of 100*");
    }

    [Fact]
    public void Submit_ShouldThrow_WhenDraftHasExpired()
    {
        var order = CreateDraftOrder();
        var expiredTime = order.ExpiresAtUtc.AddHours(1);

        Action act = () => order.Submit(DefaultOptions, expiredTime);

        act.Should().Throw<DomainException>().WithMessage("*expired*");
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenQuantityIsNotPositive()
    {
        var order = Order.Create("customer-1", new Address("1 Main St", "Seattle", "WA", "98052", "US"), TimeSpan.FromHours(24));

        Action act = () => order.AddItem("SKU-1", 0, new Money(10m, "USD"));

        act.Should().Throw<DomainException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void Expire_ShouldTransitionToExpired_WhenPastExpiration()
    {
        var order = CreateDraftOrder();

        order.Expire(order.ExpiresAtUtc.AddMinutes(1));

        order.Status.Should().Be(OrderStatus.Expired);
    }

    [Fact]
    public void Expire_ShouldNotTransition_WhenBeforeExpiration()
    {
        var order = CreateDraftOrder();

        order.Expire(order.ExpiresAtUtc.AddMinutes(-1));

        order.Status.Should().Be(OrderStatus.Draft);
    }

    private static Order CreateDraftOrder()
    {
        var order = Order.Create(
            "customer-1",
            new Address("1 Main St", "Seattle", "WA", "98052", "US"),
            TimeSpan.FromHours(24));
        order.AddItem("SKU-1", 2, new Money(25m, "USD"));
        return order;
    }
}
