using System.ComponentModel.DataAnnotations;

namespace Orion.Domain.Models;

public class OrderItem
{
    [Key]
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public int InventoryId { get; set; }
    public Inventory Inventory { get; set; } = null!;
    
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    
    // Calculated property
    public decimal TotalPrice => UnitPrice * Quantity;
}