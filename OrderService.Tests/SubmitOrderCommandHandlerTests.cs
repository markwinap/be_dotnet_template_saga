using FluentAssertions;
using Moq;
using OrderService.Application.Commands.SubmitOrder;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests;

public class SubmitOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSubmitAndPublish_WhenOrderExistsForCustomer()
    {
        var repository = new Mock<IOrderRepository>();
        var saga = new Mock<IOrderSagaOrchestrator>();
        var publisher = new Mock<IServiceBusPublisher>();
        var cache = new Mock<ICacheService>();
        var order = CreateDraftOrder();
        repository.Setup(x => x.GetByIdAsync(order.Id, order.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var handler = new SubmitOrderCommandHandler(repository.Object, saga.Object, publisher.Object, cache.Object);

        await handler.Handle(new SubmitOrderCommand(order.Id, order.CustomerId), CancellationToken.None);

        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        publisher.Invocations.Should().ContainSingle();
        publisher.Invocations[0].Arguments[0].Should().Be("orders.submitted");
        publisher.Invocations[0].Arguments[1].Should().Be(order.Id.ToString("N"));
        saga.Verify(x => x.HandleOrderSubmittedAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync($"customer:{order.CustomerId}:order:{order.Id}", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync($"customer:{order.CustomerId}:orders:list", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenOrderDoesNotBelongToCustomer()
    {
        var repository = new Mock<IOrderRepository>();
        var saga = new Mock<IOrderSagaOrchestrator>();
        var publisher = new Mock<IServiceBusPublisher>();
        var cache = new Mock<ICacheService>();
        repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), "customer-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        var handler = new SubmitOrderCommandHandler(repository.Object, saga.Object, publisher.Object, cache.Object);

        Func<Task> act = () => handler.Handle(new SubmitOrderCommand(Guid.NewGuid(), "customer-2"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        publisher.Invocations.Should().BeEmpty();
    }

    private static Order CreateDraftOrder()
    {
        var order = Order.Create("customer-1", new Address("1 Main St", "Seattle", "WA", "98052", "US"));
        order.AddItem("SKU-1", 1, new Money(10m, "USD"));
        return order;
    }
}
