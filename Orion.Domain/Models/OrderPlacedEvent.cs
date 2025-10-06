using System.ComponentModel.DataAnnotations;

namespace Orion.Domain.Models;


// Enhanced event with inventory details
public record OrderPlacedEvent(
    int OrderId,
    string UserId,
    string CustomerName,
    decimal TotalAmount,
    List<OrderItemData> Items  // NEW: Include order items
);

public record OrderItemData(
    string ProductSku,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);