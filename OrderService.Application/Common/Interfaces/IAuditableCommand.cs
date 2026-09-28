namespace OrderService.Application.Common.Interfaces;

public interface IAuditableCommand
{
    string Action { get; }
    string EntityType { get; }
    string EntityId { get; }
}
