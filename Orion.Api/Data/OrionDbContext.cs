using Microsoft.EntityFrameworkCore;
using Orion.Api.Models;
using Orion.Api.ReadModels;

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

    //event store table
    public DbSet<EventStoreEntry> EventStoreEntries { get; set; }
    
    // Read Models for CQRS
    public DbSet<OrderSummaryView> OrderSummaryViews { get; set; }
    public DbSet<OrderDetailView> OrderDetailViews { get; set; }
    public DbSet<UserOrderHistoryView> UserOrderHistoryViews { get; set; }

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

        // Event Store configuration
        modelBuilder.Entity<EventStoreEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.EventData).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes for efficient querying
            entity.HasIndex(e => e.AggregateId);
            entity.HasIndex(e => new { e.AggregateId, e.AggregateVersion }).IsUnique();
            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasIndex(e => e.OccurredAt);
        });

        // Read Models configuration
        modelBuilder.Entity<OrderSummaryView>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.SearchText).HasMaxLength(500);

            // Indexes for efficient querying
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SearchText);
        });

        modelBuilder.Entity<OrderDetailView>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.OrderItemsJson).HasColumnType("text");
            entity.Property(e => e.StatusHistoryJson).HasColumnType("text");

            // Indexes for efficient querying
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<UserOrderHistoryView>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            // Composite index for efficient user queries
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => e.Status);
        });
    }
}