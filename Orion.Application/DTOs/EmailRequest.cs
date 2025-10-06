namespace Orion.Application.DTOs;

public enum EmailType
{
    OrderConfirmation,
    OrderProcessing,
    OrderCompleted,
    OrderFailed,
    InventoryLowAlert,
    General
}

public class EmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public EmailType EmailType { get; set; }
    public string? TemplateId { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
}