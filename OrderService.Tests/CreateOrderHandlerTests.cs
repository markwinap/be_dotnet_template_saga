using FluentAssertions;
using Moq;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Features.Orders.CreateOrder;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests;

public class CreateOrderHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateOrder_WhenCommandIsValid()
    {
        var repository = new Mock<IOrderRepository>();
        var cacheService = new Mock<ICacheService>();
        var auditRepository = new Mock<IAuditRepository>();
        var auditContext = CreateAuditContext();
        var options = CreateOptions();

        var command = new CreateOrderCommand(
            "customer-1",
            "1 Main St",
            "Seattle",
            "WA",
            "98052",
            "US",
            [new CreateOrderItemRequest("SKU-1", 2, 25m, "USD")]);

        var result = await CreateOrderHandler.Handle(command, repository.Object, cacheService.Object, auditRepository.Object, auditContext, options, CancellationToken.None);

        result.CustomerId.Should().Be("customer-1");
        result.Total.Should().Be(50m);

        repository.Verify(x => x.AddAsync(It.IsAny<OrderService.Domain.Entities.Order>(), It.IsAny<CancellationToken>()), Times.Once);
        cacheService.Verify(x => x.RemoveAsync("customer:customer-1:orders:list", It.IsAny<CancellationToken>()), Times.Once);
        auditRepository.Verify(x => x.AddAsync(It.IsAny<OrderService.Domain.Entities.Audit.OrderStateTrace>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IAuditContext CreateAuditContext() =>
        Mock.Of<IAuditContext>(ctx =>
            ctx.ActorId == "customer-1" &&
            ctx.CorrelationId == "correlation-1" &&
            ctx.SourceSystem == "OrderService.Api");

    private static OrderSubmissionPolicyOptions CreateOptions() =>
        new(10m, 100, 1, 100, TimeSpan.FromHours(24), new BusinessHours(new TimeOnly(8, 0), new TimeOnly(20, 0)), TimeZoneInfo.Utc);
}
