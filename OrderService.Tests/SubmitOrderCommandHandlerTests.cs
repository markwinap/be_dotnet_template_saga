using FluentAssertions;
using Moq;
using OrderService.Application.Commands.SubmitOrder;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests;

public class SubmitOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSubmitAndPublish_WhenOrderExistsForCustomer()
    {
        var repository = new Mock<IOrderRepository>();
        var saga = new Mock<IOrderSagaOrchestrator>();
        var outboxStore = new Mock<IOutboxStore>();
        var cache = new Mock<ICacheService>();
        var submissionPolicy = new Mock<IOrderSubmissionPolicy>();
        var auditRepository = new Mock<IAuditRepository>();
        var auditContext = CreateAuditContext();
        var options = CreateOptions();
        var timeProvider = CreateTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var order = CreateDraftOrder();
        repository.Setup(x => x.GetByIdAsync(order.Id, order.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var handler = new SubmitOrderCommandHandler(
            repository.Object,
            saga.Object,
            outboxStore.Object,
            cache.Object,
            submissionPolicy.Object,
            auditRepository.Object,
            auditContext,
            options,
            timeProvider);

        await handler.Handle(new SubmitOrderCommand(order.Id, order.CustomerId), CancellationToken.None);

        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        outboxStore.Verify(x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        outboxStore.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        saga.Verify(x => x.HandleOrderSubmittedAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync($"customer:{order.CustomerId}:order:{order.Id}", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync($"customer:{order.CustomerId}:orders:list", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenOrderDoesNotBelongToCustomer()
    {
        var repository = new Mock<IOrderRepository>();
        var saga = new Mock<IOrderSagaOrchestrator>();
        var outboxStore = new Mock<IOutboxStore>();
        var cache = new Mock<ICacheService>();
        var submissionPolicy = new Mock<IOrderSubmissionPolicy>();
        var auditRepository = new Mock<IAuditRepository>();
        var auditContext = CreateAuditContext();
        var options = CreateOptions();
        var timeProvider = CreateTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), "customer-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        var handler = new SubmitOrderCommandHandler(
            repository.Object,
            saga.Object,
            outboxStore.Object,
            cache.Object,
            submissionPolicy.Object,
            auditRepository.Object,
            auditContext,
            options,
            timeProvider);

        Func<Task> act = () => handler.Handle(new SubmitOrderCommand(Guid.NewGuid(), "customer-2"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        outboxStore.Verify(x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
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
