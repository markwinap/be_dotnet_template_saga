using FluentAssertions;
using MediatR;
using Moq;
using OrderService.Application.Common.Behaviors;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Interfaces;

namespace OrderService.Tests;

public class AuditBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldWriteAuditLog_WhenRequestIsAuditable()
    {
        var auditContext = CreateAuditContext();
        var auditRepository = new Mock<IAuditRepository>();
        var behavior = new AuditBehavior<AuditableRequest, string>(auditContext, auditRepository.Object);

        var result = await behavior.Handle(new AuditableRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        auditRepository.Verify(x => x.AddAsync(It.Is<AuditLog>(log =>
            log.Action == "TestAction" &&
            log.EntityType == "Order" &&
            log.EntityId == "order-1" &&
            log.ActorId == "customer-1"), It.IsAny<CancellationToken>()), Times.Once);
        auditRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotWriteAuditLog_WhenRequestIsNotAuditable()
    {
        var auditContext = CreateAuditContext();
        var auditRepository = new Mock<IAuditRepository>();
        var behavior = new AuditBehavior<NonAuditableRequest, string>(auditContext, auditRepository.Object);

        var result = await behavior.Handle(new NonAuditableRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        auditRepository.Verify(x => x.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IAuditContext CreateAuditContext() =>
        Mock.Of<IAuditContext>(ctx =>
            ctx.ActorId == "customer-1" &&
            ctx.ActorEmail == "customer@example.com" &&
            ctx.CorrelationId == "correlation-1" &&
            ctx.SourceSystem == "OrderService.Api" &&
            ctx.ClientIp == "127.0.0.1" &&
            ctx.SessionId == "session-1");

    private sealed record AuditableRequest : IAuditableCommand
    {
        public string Action => "TestAction";
        public string EntityType => "Order";
        public string EntityId => "order-1";
    }

    private sealed record NonAuditableRequest;
}
