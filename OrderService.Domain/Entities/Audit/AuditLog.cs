using System.Security.Cryptography;
using System.Text;

namespace OrderService.Domain.Entities.Audit;

public sealed class AuditLog
{
    private AuditLog() { }

    public Guid EventId { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public string ActorId { get; private set; } = string.Empty;
    public string? ActorEmail { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string? Reason { get; private set; }
    public string SourceSystem { get; private set; } = "OrderService";
    public string CorrelationId { get; private set; } = string.Empty;
    public string? ClientIp { get; private set; }
    public string? SessionId { get; private set; }
    public string RowHash { get; private set; } = string.Empty;

    public static AuditLog Create(
        string actorId,
        string? actorEmail,
        string action,
        string entityType,
        string entityId,
        string? beforeJson,
        string? afterJson,
        string? reason,
        string sourceSystem,
        string correlationId,
        string? clientIp,
        string? sessionId)
    {
        var log = new AuditLog
        {
            EventId = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            ActorId = actorId,
            ActorEmail = actorEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            Reason = reason,
            SourceSystem = sourceSystem,
            CorrelationId = correlationId,
            ClientIp = clientIp,
            SessionId = sessionId
        };

        log.RowHash = log.ComputeRowHash();
        return log;
    }

    private string ComputeRowHash()
    {
        var content =
            $"{EventId:N}|{TimestampUtc:O}|{ActorId}|{Action}|{EntityType}|{EntityId}|{BeforeJson}|{AfterJson}|{Reason}|{SourceSystem}|{CorrelationId}|{ClientIp}|{SessionId}";
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
