using System.ComponentModel.DataAnnotations;

namespace Orion.Application.DTOs;

public record OrderItemResponse(
    string ProductName,
    string ProductSku,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);