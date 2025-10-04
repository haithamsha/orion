using System.ComponentModel.DataAnnotations;
using Orion.Api.Models;

namespace Orion.Api.ReadModels;

/// <summary>
/// Optimized read model for order list views and summaries
/// </summary>
public class OrderSummaryView
{
    [Key]
    public int OrderId { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    
    // Aggregated information for quick access
    public int ItemCount { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public string FormattedAmount { get; set; } = string.Empty;
    
    // Quick filters
    public bool IsCompleted { get; set; }
    public bool IsPending { get; set; }
    public bool IsFailed { get; set; }
    
    // For search and filtering
    public string SearchText { get; set; } = string.Empty;
}