namespace Orion.Api.Models.Events;

public record OrderCreatedEvent : BaseEvent
{
    public int OrderId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public List<OrderItemData> Items { get; init; } = new();
    
    public OrderCreatedEvent()
    {
        EventType = nameof(OrderCreatedEvent);
    }
}

public record OrderProcessingStartedEvent : BaseEvent
{
    public int OrderId { get; init; }
    
    public OrderProcessingStartedEvent()
    {
        EventType = nameof(OrderProcessingStartedEvent);
    }
}

public record OrderCompletedEvent : BaseEvent
{
    public int OrderId { get; init; }
    public DateTime CompletedAt { get; init; }
    
    public OrderCompletedEvent()
    {
        EventType = nameof(OrderCompletedEvent);
    }
}

public record OrderFailedEvent : BaseEvent
{
    public int OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    
    public OrderFailedEvent()
    {
        EventType = nameof(OrderFailedEvent);
    }
}

public record OrderItemData(
    string ProductSku,
    int Quantity,
    decimal UnitPrice
);