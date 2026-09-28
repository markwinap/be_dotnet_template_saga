using FluentAssertions;
using Moq;
using OrderService.Application.Commands.DeleteOrder;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
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
        var cacheService = new Mock<ICacheService>();
        var auditRepository = new Mock<IAuditRepository>();
        var auditContext = CreateAuditContext();
        var existingOrder = CreateDraftOrder();

        repository.Setup(x => x.GetByIdAsync(existingOrder.Id, existingOrder.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        var handler = new DeleteOrderCommandHandler(repository.Object, cacheService.Object, auditRepository.Object, auditContext);

        await handler.Handle(new DeleteOrderCommand(existingOrder.Id, existingOrder.CustomerId), CancellationToken.None);

        repository.Verify(x => x.Remove(existingOrder), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cacheService.Verify(x => x.RemoveAsync($"customer:{existingOrder.CustomerId}:order:{existingOrder.Id}", It.IsAny<CancellationToken>()), Times.Once);
        cacheService.Verify(x => x.RemoveAsync($"customer:{existingOrder.CustomerId}:orders:list", It.IsAny<CancellationToken>()), Times.Once);
        auditRepository.Verify(x => x.AddAsync(It.IsAny<OrderService.Domain.Entities.Audit.OrderStateTrace>(), It.IsAny<CancellationToken>()), Times.Once);
        auditRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenOrderDoesNotExist()
    {
        var repository = new Mock<IOrderRepository>();
        var cacheService = new Mock<ICacheService>();
        var auditRepository = new Mock<IAuditRepository>();
        var auditContext = CreateAuditContext();
        repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), "customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = new DeleteOrderCommandHandler(repository.Object, cacheService.Object, auditRepository.Object, auditContext);

        Func<Task> act = async () => await handler.Handle(new DeleteOrderCommand(Guid.NewGuid(), "customer-1"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        repository.Verify(x => x.Remove(It.IsAny<Order>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IAuditContext CreateAuditContext() =>
        Mock.Of<IAuditContext>(ctx =>
            ctx.ActorId == "customer-1" &&
            ctx.CorrelationId == "correlation-1" &&
            ctx.SourceSystem == "OrderService.Api");

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
