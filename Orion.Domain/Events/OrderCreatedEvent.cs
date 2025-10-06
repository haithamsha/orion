using Orion.Domain.Models;

namespace Orion.Domain.Events;

public record OrderCreatedEvent : BaseEvent
{
    public string CustomerUserId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string CustomerName { get; init; } = string.Empty;
    public List<OrderItemData> OrderItems { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; } = OrderStatus.Pending;

    public OrderCreatedEvent(Guid aggregateId, int aggregateVersion, string customerUserId, string customerName, decimal totalAmount, List<OrderItemData> orderItems, string? userId = null)
        : base(aggregateId, aggregateVersion, userId)
    {
        CustomerUserId = customerUserId;
        CustomerName = customerName;
        TotalAmount = totalAmount;
        OrderItems = orderItems;
        CreatedAt = DateTime.UtcNow;
    }
}

public record OrderItemData
{
    public string ProductSku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Total => Quantity * UnitPrice;
}
