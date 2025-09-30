using System.ComponentModel.DataAnnotations;

namespace Orion.Api.Models;


public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class Order
{
    [Key]
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // NEW: The user who placed the order
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}