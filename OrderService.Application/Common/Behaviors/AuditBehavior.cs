using System.Text.Json;
using MediatR;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Common.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditContext auditContext,
    IAuditRepository auditRepository) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableCommand auditable)
        {
            var afterJson = JsonSerializer.Serialize(request, request.GetType(), JsonOptions);
            var auditLog = AuditLog.Create(
                auditContext.ActorId,
                auditContext.ActorEmail,
                auditable.Action,
                auditable.EntityType,
                auditable.EntityId,
                null,
                afterJson,
                null,
                auditContext.SourceSystem,
                auditContext.CorrelationId,
                auditContext.ClientIp,
                auditContext.SessionId);

            await auditRepository.AddAsync(auditLog, cancellationToken);
            await auditRepository.SaveChangesAsync(cancellationToken);
        }

        return response;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
