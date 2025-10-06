using System.ComponentModel.DataAnnotations;

namespace Orion.Domain.DTOs;



public record OrderItemResponse(
    string ProductName,
    string ProductSku,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);