namespace Orion.Api.Models.DTOs;

public enum EmailType
{
    OrderConfirmation,
    OrderProcessing,
    OrderCompleted,
    OrderFailed,
    PaymentFailed,
    InventoryLow
}

public record EmailRequest(
    string ToEmail,
    string ToName,
    EmailType EmailType,
    Dictionary<string, object> TemplateData
);

public record OrderEmailData(
    int OrderId,
    string CustomerName,
    decimal TotalAmount,
    DateTime OrderDate,
    List<OrderItemResponse> Items,
    string Status
);

public record EmailSentEvent(
    int OrderId,
    string UserId,
    string EmailType,
    string ToEmail,
    bool Success,
    string? ErrorMessage = null
);