using FluentAssertions;
using Moq;
using OrderService.Application.Queries.ListOrders;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests;

public class ListOrdersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMappedOrders()
    {
        var repository = new Mock<IOrderRepository>();
        var orders = new[]
        {
            CreateDraftOrder("customer-1", "SKU-1", 2, 25m),
            CreateDraftOrder("customer-2", "SKU-2", 1, 15m)
        };

        repository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var handler = new ListOrdersQueryHandler(repository.Object);

        var result = await handler.Handle(new ListOrdersQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.CustomerId).Should().Contain(["customer-1", "customer-2"]);
        result.Sum(x => x.Total).Should().Be(65m);
        repository.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Order CreateDraftOrder(string customerId, string sku, int quantity, decimal unitPrice)
    {
        var order = Order.Create(
            customerId,
            new Address("1 Main St", "Seattle", "WA", "98052", "US"));
        order.AddItem(sku, quantity, new Money(unitPrice, "USD"));
        return order;
    }
}
