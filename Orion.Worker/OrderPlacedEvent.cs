// Enhanced event contract - must match the API exactly
public record OrderPlacedEvent(
    int OrderId, 
    string UserId, 
    string CustomerName, 
    decimal TotalAmount,
    List<OrderItemData> Items
);

public record OrderItemData(
    string ProductSku,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);