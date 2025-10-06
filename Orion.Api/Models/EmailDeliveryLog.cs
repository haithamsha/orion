namespace Orion.Api.Models;

public class EmailDeliveryLog
{
    public int Id { get; set; }
    public int? OrderId { get; set; }
    public string EmailType { get; set; } = "";
    public string ToEmail { get; set; } = "";
    public string ToName { get; set; } = "";
    public string? TemplateId { get; set; }
    public string Provider { get; set; } = "";
    public bool Success { get; set; }
    public int AttemptCount { get; set; } = 1;
    public double TotalDeliveryTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Order? Order { get; set; }
}