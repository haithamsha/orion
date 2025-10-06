using System.ComponentModel.DataAnnotations;
using Orion.Domain.Models;

namespace Orion.Infrastructure.ReadModels;

/// <summary>
/// Detailed read model for individual order views with complete history
/// </summary>
public class OrderDetailView
{
    [Key]
    public int OrderId { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    
    // JSON serialized order items for quick access
    public string OrderItemsJson { get; set; } = string.Empty;
    
    // Status history (JSON serialized for quick access)
    public string StatusHistoryJson { get; set; } = string.Empty;
    
    // Computed fields
    public TimeSpan Age { get; set; }
    public string AgeDisplay { get; set; } = string.Empty;
}

/// <summary>
/// User-specific order history for fast user queries
/// </summary>
public class UserOrderHistoryView
{
    [Key]
    public string Id { get; set; } = string.Empty; // UserId + OrderId combination
    
    public string UserId { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    
    // For efficient user-based queries
    public int ItemCount { get; set; }
    public string FormattedAmount { get; set; } = string.Empty;
    public bool IsRecent { get; set; }
}