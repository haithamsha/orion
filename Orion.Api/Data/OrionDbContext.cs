using Microsoft.EntityFrameworkCore;
using Orion.Api.Models;

namespace Orion.Api.Data;

public class OrionDbContext : DbContext
{
    public OrionDbContext(DbContextOptions<OrionDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

    // Existing configurations...
    modelBuilder.Entity<OrderItem>()
        .HasOne(oi => oi.Order)
        .WithMany(o => o.OrderItems)
        .HasForeignKey(oi => oi.OrderId);

    modelBuilder.Entity<OrderItem>()
        .HasOne(oi => oi.Inventory)
        .WithMany()
        .HasForeignKey(oi => oi.InventoryId);

    modelBuilder.Entity<Inventory>()
        .Property(i => i.Price)
        .HasPrecision(18, 2);

    modelBuilder.Entity<OrderItem>()
        .Property(oi => oi.UnitPrice)
        .HasPrecision(18, 2);

    modelBuilder.Entity<Inventory>()
        .HasIndex(i => i.ProductSku)
        .IsUnique();

    // NEW: User model configurations
    modelBuilder.Entity<User>()
        .HasKey(u => u.Id); // Primary key is Id
        
    modelBuilder.Entity<User>()
        .HasIndex(u => u.UserId)
        .IsUnique(); // Ensure UserId (from JWT) is unique

    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique(); // Ensure email is unique

    // NEW: Configure User-Order relationship
    // Order.UserId (string) -> User.UserId (string) 
    modelBuilder.Entity<Order>()
        .HasOne<User>()
        .WithMany(u => u.Orders)
        .HasForeignKey(o => o.UserId)
        .HasPrincipalKey(u => u.UserId); // Use UserId string instead of Id int
    }
}