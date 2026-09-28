namespace OrderService.Application.Common.Interfaces;

public interface IAuditContext
{
    string ActorId { get; }
    string? ActorEmail { get; }
    string? ClientIp { get; }
    string? SessionId { get; }
    string CorrelationId { get; }
    string SourceSystem { get; }
}
