using System.ComponentModel.DataAnnotations;

namespace Orion.Api.Models;

public class Inventory
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    public string ProductSku { get; set; } = string.Empty; // Stock Keeping Unit
    
    public decimal Price { get; set; }
    
    public int AvailableQuantity { get; set; }
    
    public int ReservedQuantity { get; set; } // Items reserved for pending orders
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Calculated property
    public int TotalStock => AvailableQuantity + ReservedQuantity;
}