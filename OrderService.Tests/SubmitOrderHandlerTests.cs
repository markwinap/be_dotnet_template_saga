using FluentAssertions;
using Moq;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Features.Orders.SubmitOrder;
using OrderService.Application.Messages;
using OrderService.Application.Messages.Saga;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests;

public class SubmitOrderHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSubmitAndReturnMessages_WhenOrderExistsForCustomer()
    {
        var repository = new Mock<IOrderRepository>();
        var cache = new Mock<ICacheService>();
        var submissionPolicy = new Mock<IOrderSubmissionPolicy>();
        var auditRepository = new Mock<IAuditRepository>();
        var auditContext = CreateAuditContext();
        var options = CreateOptions();
        var timeProvider = CreateTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var order = CreateDraftOrder();
        repository.Setup(x => x.GetByIdAsync(order.Id, order.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await SubmitOrderHandler.Handle(
            new SubmitOrderCommand(order.Id, order.CustomerId),
            repository.Object,
            submissionPolicy.Object,
            auditRepository.Object,
            auditContext,
            options,
            timeProvider,
            cache.Object,
            CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(m => m is OrderSubmittedEvent);
        result.Should().Contain(m => m is OrderSubmitted);
        cache.Verify(x => x.RemoveAsync($"customer:{order.CustomerId}:order:{order.Id}", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync($"customer:{order.CustomerId}:orders:list", It.IsAny<CancellationToken>()), Times.Once);
        auditRepository.Verify(x => x.AddAsync(It.IsAny<OrderService.Domain.Entities.Audit.OrderStateTrace>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenOrderDoesNotBelongToCustomer()
    {
        var repository = new Mock<IOrderRepository>();
        var cache = new Mock<ICacheService>();
        var submissionPolicy = new Mock<IOrderSubmissionPolicy>();
        var auditRepository = new Mock<IAuditRepository>();
        var auditContext = CreateAuditContext();
        var options = CreateOptions();
        var timeProvider = CreateTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), "customer-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        Func<Task> act = () => SubmitOrderHandler.Handle(
            new SubmitOrderCommand(Guid.NewGuid(), "customer-2"),
            repository.Object,
            submissionPolicy.Object,
            auditRepository.Object,
            auditContext,
            options,
            timeProvider,
            cache.Object,
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static IAuditContext CreateAuditContext() =>
        Mock.Of<IAuditContext>(ctx =>
            ctx.ActorId == "customer-1" &&
            ctx.CorrelationId == "correlation-1" &&
            ctx.SourceSystem == "OrderService.Api");

    private static OrderSubmissionPolicyOptions CreateOptions() =>
        new(10m, 100, 1, 100, TimeSpan.FromHours(24), new BusinessHours(new TimeOnly(8, 0), new TimeOnly(20, 0)), TimeZoneInfo.Utc);

    private static Order CreateDraftOrder()
    {
        var order = Order.Create("customer-1", new Address("1 Main St", "Seattle", "WA", "98052", "US"), TimeSpan.FromHours(24));
        order.AddItem("SKU-1", 1, new Money(10m, "USD"));
        return order;
    }

    private static TimeProvider CreateTimeProvider(DateTimeOffset now)
    {
        var mock = new Mock<TimeProvider>();
        mock.Setup(x => x.GetUtcNow()).Returns(now);
        return mock.Object;
    }
}
