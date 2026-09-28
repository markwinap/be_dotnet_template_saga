using FluentAssertions;
using Moq;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Features.Orders.DeleteOrder;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests;

public class DeleteOrderHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDeleteOrder_WhenOrderExists()
    {
        var repository = new Mock<IOrderRepository>();
        var cacheService = new Mock<ICacheService>();
        var existingOrder = CreateDraftOrder();

        repository.Setup(x => x.GetByIdAsync(existingOrder.Id, existingOrder.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        await DeleteOrderHandler.Handle(
            new DeleteOrderCommand(existingOrder.Id, existingOrder.CustomerId),
            repository.Object,
            cacheService.Object,
            CancellationToken.None);

        repository.Verify(x => x.Remove(existingOrder), Times.Once);
        cacheService.Verify(x => x.RemoveAsync($"customer:{existingOrder.CustomerId}:order:{existingOrder.Id}", It.IsAny<CancellationToken>()), Times.Once);
        cacheService.Verify(x => x.RemoveAsync($"customer:{existingOrder.CustomerId}:orders:list", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenOrderDoesNotExist()
    {
        var repository = new Mock<IOrderRepository>();
        var cacheService = new Mock<ICacheService>();
        repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), "customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        Func<Task> act = () => DeleteOrderHandler.Handle(
            new DeleteOrderCommand(Guid.NewGuid(), "customer-1"),
            repository.Object,
            cacheService.Object,
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        repository.Verify(x => x.Remove(It.IsAny<Order>()), Times.Never);
    }

    private static Order CreateDraftOrder()
    {
        var order = Order.Create(
            "customer-1",
            new Address("1 Main St", "Seattle", "WA", "98052", "US"),
            TimeSpan.FromHours(24));
        order.AddItem("SKU-1", 1, new Money(10m, "USD"));
        return order;
    }
}
