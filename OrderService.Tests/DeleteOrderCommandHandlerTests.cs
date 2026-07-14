using FluentAssertions;
using Moq;
using OrderService.Application.Commands.DeleteOrder;
using OrderService.Application.Common.Exceptions;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests;

public class DeleteOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDeleteOrder_WhenOrderExists()
    {
        var repository = new Mock<IOrderRepository>();
        var existingOrder = CreateDraftOrder();

        repository.Setup(x => x.GetByIdAsync(existingOrder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        var handler = new DeleteOrderCommandHandler(repository.Object);

        await handler.Handle(new DeleteOrderCommand(existingOrder.Id), CancellationToken.None);

        repository.Verify(x => x.Remove(existingOrder), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenOrderDoesNotExist()
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = new DeleteOrderCommandHandler(repository.Object);

        Func<Task> act = async () => await handler.Handle(new DeleteOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        repository.Verify(x => x.Remove(It.IsAny<Order>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Order CreateDraftOrder()
    {
        var order = Order.Create(
            "customer-1",
            new Address("1 Main St", "Seattle", "WA", "98052", "US"));
        order.AddItem("SKU-1", 1, new Money(10m, "USD"));
        return order;
    }
}
