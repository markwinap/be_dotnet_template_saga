using FluentAssertions;
using Moq;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Common.Middleware;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Interfaces;
using Wolverine;

namespace OrderService.Tests;

public class AuditMiddlewareTests
{
    [Fact]
    public async Task AfterCommit_ShouldWriteAuditLog_WhenMessageIsAuditable()
    {
        var auditContext = CreateAuditContext();
        var auditRepository = new Mock<IAuditRepository>();
        var envelope = new Envelope(new AuditableRequest());

        await AuditMiddleware.AfterAsync(envelope, auditContext, auditRepository.Object, CancellationToken.None);

        auditRepository.Verify(x => x.AddAsync(It.Is<AuditLog>(log =>
            log.Action == "TestAction" &&
            log.EntityType == "Order" &&
            log.EntityId == "order-1" &&
            log.ActorId == "customer-1"), It.IsAny<CancellationToken>()), Times.Once);
        auditRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AfterCommit_ShouldNotWriteAuditLog_WhenMessageIsNotAuditable()
    {
        var auditContext = CreateAuditContext();
        var auditRepository = new Mock<IAuditRepository>();
        var envelope = new Envelope(new NonAuditableRequest());

        await AuditMiddleware.AfterAsync(envelope, auditContext, auditRepository.Object, CancellationToken.None);

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
