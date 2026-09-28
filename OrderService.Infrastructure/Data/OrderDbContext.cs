using Microsoft.EntityFrameworkCore;
using OrderService.Application.Sagas;
using OrderService.Domain.Entities;
using OrderService.Domain.Entities.Audit;

namespace OrderService.Infrastructure.Data;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OrderStateTrace> OrderStateTraces => Set<OrderStateTrace>();
    public DbSet<OrderProcessingSaga> OrderWorkflowExecutions => Set<OrderProcessingSaga>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("Orders");
            order.HasKey(o => o.Id);
            order.Property(o => o.OrderNumber).HasMaxLength(64);
            order.Property(o => o.CustomerId).HasMaxLength(64);
            order.Property(o => o.RowVersion).IsRowVersion();
            order.Property(o => o.SubmittedAtUtc);
            order.Property(o => o.ExpiresAtUtc);
            order.HasIndex(o => new { o.CustomerId, o.CreatedAtUtc });
            order.HasIndex(o => o.OrderNumber).IsUnique();
            order.HasIndex(o => new { o.CustomerId, o.SubmittedAtUtc });
            order.OwnsOne(o => o.ShippingAddress, address =>
            {
                address.Property(a => a.Line1).HasMaxLength(200);
                address.Property(a => a.City).HasMaxLength(100);
                address.Property(a => a.State).HasMaxLength(100);
                address.Property(a => a.PostalCode).HasMaxLength(20);
                address.Property(a => a.Country).HasMaxLength(100);
            });
            order.Ignore(o => o.DomainEvents);
            order.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
        });

        modelBuilder.Entity<OrderItem>(item =>
        {
            item.ToTable("OrderItems");
            item.HasKey(x => x.Id);
            item.Property(x => x.ProductCode).HasMaxLength(64);
            item.OwnsOne(x => x.UnitPrice, money =>
            {
                money.Property(m => m.Amount).HasColumnType("decimal(18,2)");
                money.Property(m => m.Currency).HasMaxLength(3);
            });
        });

        modelBuilder.Entity<OrderProcessingSaga>(execution =>
        {
            execution.ToTable("OrderWorkflowExecutions", "dbo");
            execution.HasKey(x => x.Id);
            execution.Property(x => x.CurrentStep).HasMaxLength(128);
            execution.Property(x => x.Status).HasMaxLength(32);
            execution.Property(x => x.ErrorReason).HasMaxLength(500);
            execution.HasIndex(x => x.Id).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(audit =>
        {
            audit.ToTable("AuditLog", "audit");
            audit.HasKey(x => x.EventId);
            audit.Property(x => x.ActorId).HasMaxLength(64);
            audit.Property(x => x.ActorEmail).HasMaxLength(128);
            audit.Property(x => x.Action).HasMaxLength(64);
            audit.Property(x => x.EntityType).HasMaxLength(64);
            audit.Property(x => x.EntityId).HasMaxLength(100);
            audit.Property(x => x.Reason).HasMaxLength(500);
            audit.Property(x => x.SourceSystem).HasMaxLength(64);
            audit.Property(x => x.CorrelationId).HasMaxLength(100);
            audit.Property(x => x.ClientIp).HasMaxLength(64);
            audit.Property(x => x.SessionId).HasMaxLength(128);
            audit.Property(x => x.RowHash).HasMaxLength(64);
            audit.HasIndex(x => new { x.EntityId, x.TimestampUtc });
            audit.HasIndex(x => new { x.ActorId, x.TimestampUtc });
            audit.HasIndex(x => x.CorrelationId);
            audit.HasIndex(x => x.Action);
        });

        modelBuilder.Entity<OrderStateTrace>(trace =>
        {
            trace.ToTable("OrderStateTrace", "audit");
            trace.HasKey(x => x.TraceId);
            trace.Property(x => x.State).HasMaxLength(64);
            trace.Property(x => x.PreviousState).HasMaxLength(64);
            trace.Property(x => x.ChangedById).HasMaxLength(64);
            trace.Property(x => x.ChangeReason).HasMaxLength(500);
            trace.Property(x => x.CorrelationId).HasMaxLength(100);
            trace.HasIndex(x => new { x.OrderId, x.ChangedAtUtc });
            trace.HasIndex(x => new { x.ChangedById, x.ChangedAtUtc });
            trace.HasIndex(x => x.State);
        });
    }
}
