using System.ComponentModel.DataAnnotations;

namespace Orion.Api.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty; // This matches the JWT 'sub' claim
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    public string? PhoneNumber { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property for orders
    public List<Order> Orders { get; set; } = new();
    
    // Calculated property
    public string FullName => $"{FirstName} {LastName}";
}