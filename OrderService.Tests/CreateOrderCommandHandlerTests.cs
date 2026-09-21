using FluentAssertions;
using Moq;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Interfaces;

namespace OrderService.Tests;

public class CreateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateOrder_WhenCommandIsValid()
    {
        var repository = new Mock<IOrderRepository>();
        var cacheService = new Mock<ICacheService>();
        var handler = new CreateOrderCommandHandler(repository.Object, cacheService.Object);

        var command = new CreateOrderCommand(
            "customer-1",
            "1 Main St",
            "Seattle",
            "WA",
            "98052",
            "US",
            [new CreateOrderItemRequest("SKU-1", 2, 25m, "USD")]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.CustomerId.Should().Be("customer-1");
        result.Total.Should().Be(50m);

        repository.Verify(x => x.AddAsync(It.IsAny<OrderService.Domain.Entities.Order>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cacheService.Verify(x => x.RemoveAsync("customer:customer-1:orders:list", It.IsAny<CancellationToken>()), Times.Once);
    }
}
