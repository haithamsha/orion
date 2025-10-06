namespace Orion.Api.Models;

public class EmailDeliveryAttempt
{
    public int Id { get; set; }
    public int? OrderId { get; set; }
    public string ProviderName { get; set; } = "";
    public bool Success { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public double DeliveryTimeMs { get; set; }

    // Navigation property
    public Order? Order { get; set; }
}