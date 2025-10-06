using System.ComponentModel.DataAnnotations;

namespace Orion.Application.DTOs;

public record OrderResponse(
    int OrderId,
    string UserId,
    string CustomerName,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    List<OrderItemResponse> Items
);