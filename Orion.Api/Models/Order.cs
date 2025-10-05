using System.ComponentModel.DataAnnotations;

namespace Orion.Api.Models;


public enum OrderStatus
{
    Pending,
    InventoryReserved,    // NEW: Inventory has been reserved
    Processing,
    InventoryConfirmed,   // NEW: Inventory reduction confirmed
    Completed,
    Failed,
    InventoryRollback     // NEW: Inventory was rolled back due to failure
}

public class Order
{
    [Key]
    public int Id { get; set; }
    public Guid AggregateId { get; set; } // NEW: Link to the event sourcing aggregate
    public string UserId { get; set; } = string.Empty; // NEW: The user who placed the order
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // NEW: Navigation property for order items
    public List<OrderItem> OrderItems { get; set; } = new();
}