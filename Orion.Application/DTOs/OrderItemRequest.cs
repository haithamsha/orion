using System.ComponentModel.DataAnnotations;

namespace Orion.Application.DTOs;

public record OrderItemRequest(
    [Required] string ProductSku,
    [Range(1, int.MaxValue)] int Quantity
);
