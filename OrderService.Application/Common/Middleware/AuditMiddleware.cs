using System.Text.Json;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Interfaces;
using Wolverine;

namespace OrderService.Application.Common.Middleware;

public static class AuditMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task AfterAsync(
        Envelope envelope,
        IAuditContext auditContext,
        IAuditRepository auditRepository,
        CancellationToken cancellationToken)
    {
        if (envelope.Message is not IAuditableCommand command)
        {
            return;
        }

        var log = AuditLog.Create(
            auditContext.ActorId,
            auditContext.ActorEmail,
            command.Action,
            command.EntityType,
            command.EntityId,
            null,
            JsonSerializer.Serialize(envelope.Message, envelope.Message.GetType(), JsonOptions),
            null,
            auditContext.SourceSystem,
            auditContext.CorrelationId,
            auditContext.ClientIp,
            auditContext.SessionId);

        await auditRepository.AddAsync(log, cancellationToken);
        await auditRepository.SaveChangesAsync(cancellationToken);
    }
}
