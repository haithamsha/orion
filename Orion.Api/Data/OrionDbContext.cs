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
    
    // ADD ONLY THESE TWO NEW DbSets:
    public DbSet<EmailDeliveryLog> EmailDeliveryLogs { get; set; }
    public DbSet<EmailDeliveryAttempt> EmailDeliveryAttempts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // KEEP ALL YOUR EXISTING CONFIGURATIONS EXACTLY AS THEY ARE:
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

        // ADD ONLY THESE NEW EMAIL ENTITY CONFIGURATIONS:

        // Email Delivery Log configuration
        modelBuilder.Entity<EmailDeliveryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmailType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ToEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ToName).HasMaxLength(255);
            entity.Property(e => e.TemplateId).HasMaxLength(100);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Optional relationship to Order
            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ToEmail);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Email Delivery Attempt configuration
        modelBuilder.Entity<EmailDeliveryAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProviderName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.AttemptedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Optional relationship to Order
            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => e.AttemptedAt);
        });
    }
}