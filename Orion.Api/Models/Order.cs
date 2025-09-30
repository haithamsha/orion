using System.ComponentModel.DataAnnotations;

namespace Orion.Api.Models;

public class Order
{
    [Key]
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}