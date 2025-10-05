using Orion.Api.Models;

namespace Orion.Api.Models.Events;

/// <summary>
/// Event raised when a new order is created in the system
/// Contains all the initial data needed to reconstruct the order state
/// </summary>
public record OrderCreatedEvent : BaseEvent
{
    /// <summary>
    /// The customer's user ID who placed the order
    /// </summary>
    public string CustomerUserId { get; init; } = string.Empty;

    /// <summary>
    /// Total amount of the order
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Currency of the order (default USD)
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Initial status of the order (usually Pending)
    /// </summary>
    public OrderStatus Status { get; init; }

    /// <summary>
    /// List of items in the order
    /// </summary>
    public List<OrderItemDataDetailed> Items { get; init; } = new();

    /// <summary>
    /// Customer's notes for the order
    /// </summary>
    public string? Notes { get; init; }

    public OrderCreatedEvent(
        Guid orderId, 
        string customerUserId, 
        decimal totalAmount, 
        List<OrderItemDataDetailed> items,
        string? notes = null,
        string currency = "USD",
        OrderStatus status = OrderStatus.Pending,
        string? triggeredByUserId = null) 
        : base(orderId, 1, triggeredByUserId)
    {
        CustomerUserId = customerUserId;
        TotalAmount = totalAmount;
        Currency = currency;
        Status = status;
        Items = items;
        Notes = notes;
    }

    // Parameterless constructor for JSON deserialization
    public OrderCreatedEvent() : base(Guid.Empty, 0, null)
    {
    }
}

/// <summary>
/// Event raised when an order's status changes
/// </summary>
public record OrderStatusChangedEvent : BaseEvent
{
    /// <summary>
    /// The previous status of the order
    /// </summary>
    public OrderStatus PreviousStatus { get; init; }

    /// <summary>
    /// The new status of the order
    /// </summary>
    public OrderStatus NewStatus { get; init; }

    /// <summary>
    /// Optional reason for the status change
    /// </summary>
    public string? Reason { get; init; }

    public OrderStatusChangedEvent(
        Guid orderId,
        int aggregateVersion,
        OrderStatus previousStatus,
        OrderStatus newStatus,
        string? reason = null,
        string? triggeredByUserId = null)
        : base(orderId, aggregateVersion, triggeredByUserId)
    {
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Reason = reason;
    }

    // Parameterless constructor for JSON deserialization
    public OrderStatusChangedEvent() : base(Guid.Empty, 0, null)
    {
    }
}

/// <summary>
/// Event raised when an order is completed successfully
/// </summary>
public record OrderCompletedEvent : BaseEvent
{
    /// <summary>
    /// When the order was completed
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Optional completion notes
    /// </summary>
    public string? CompletionNotes { get; init; }

    public OrderCompletedEvent(
        Guid orderId, 
        int aggregateVersion,
        DateTime completedAt,
        string? completionNotes = null,
        string? triggeredByUserId = null) 
        : base(orderId, aggregateVersion, triggeredByUserId)
    {
        CompletedAt = completedAt;
        CompletionNotes = completionNotes;
    }

    // Parameterless constructor for JSON deserialization
    public OrderCompletedEvent() : base(Guid.Empty, 0, null)
    {
    }
}

/// <summary>
/// Event raised when an order processing fails
/// </summary>
public record OrderFailedEvent : BaseEvent
{
    /// <summary>
    /// The reason for the failure
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// When the failure occurred
    /// </summary>
    public DateTime FailedAt { get; init; }

    /// <summary>
    /// Whether this failure is recoverable
    /// </summary>
    public bool IsRecoverable { get; init; }

    public OrderFailedEvent(
        Guid orderId, 
        int aggregateVersion,
        string reason, 
        DateTime failedAt,
        bool isRecoverable = true,
        string? triggeredByUserId = null) 
        : base(orderId, aggregateVersion, triggeredByUserId)
    {
        Reason = reason;
        FailedAt = failedAt;
        IsRecoverable = isRecoverable;
    }

    // Parameterless constructor for JSON deserialization
    public OrderFailedEvent() : base(Guid.Empty, 0, null)
    {
    }
}

/// <summary>
/// Event raised when order processing starts
/// </summary>
public record OrderProcessingStartedEvent : BaseEvent
{
    /// <summary>
    /// When processing started
    /// </summary>
    public DateTime ProcessingStartedAt { get; init; }

    public OrderProcessingStartedEvent(
        Guid orderId, 
        int aggregateVersion,
        DateTime processingStartedAt,
        string? triggeredByUserId = null) 
        : base(orderId, aggregateVersion, triggeredByUserId)
    {
        ProcessingStartedAt = processingStartedAt;
    }

    // Parameterless constructor for JSON deserialization
    public OrderProcessingStartedEvent() : base(Guid.Empty, 0, null)
    {
    }
}

/// <summary>
/// Represents an item within an order for event sourcing
/// This is a value object that gets serialized with the event
/// Extended version of the base OrderItemData with inventory tracking
/// </summary>
public record OrderItemDataDetailed
{
    public Guid InventoryId { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }

    // Constructor from base OrderItemData
    public OrderItemDataDetailed(OrderItemData baseItem, Guid inventoryId)
    {
        InventoryId = inventoryId;
        ProductSku = baseItem.ProductSku;
        ProductName = baseItem.ProductName;
        Quantity = baseItem.Quantity;
        UnitPrice = baseItem.UnitPrice;
        TotalPrice = baseItem.Quantity * baseItem.UnitPrice;
    }

    // Default constructor for serialization
    public OrderItemDataDetailed() { }
}