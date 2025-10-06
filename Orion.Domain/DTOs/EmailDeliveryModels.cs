namespace Orion.Domain.DTOs;

// Email Delivery Request Model
public record EmailDeliveryRequest
{
    public string ToEmail { get; init; } = "";
    public string ToName { get; init; } = "";
    public string Subject { get; init; } = "";
    public string TemplateId { get; init; } = "";
    public object TemplateData { get; init; } = new();
}


