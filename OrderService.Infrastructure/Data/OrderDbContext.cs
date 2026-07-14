using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Data;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.HasKey(o => o.Id);
            order.Property(o => o.OrderNumber).HasMaxLength(64);
            order.Property(o => o.CustomerId).HasMaxLength(64);
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
            item.HasKey(x => x.Id);
            item.Property(x => x.ProductCode).HasMaxLength(64);
            item.OwnsOne(x => x.UnitPrice, money =>
            {
                money.Property(m => m.Amount).HasColumnType("decimal(18,2)");
                money.Property(m => m.Currency).HasMaxLength(3);
            });
        });
    }
}
