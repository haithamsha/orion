using System.ComponentModel.DataAnnotations;

namespace Orion.Domain.DTOs;

public record OrderItemRequest(
    [Required] string ProductSku,
    [Range(1, int.MaxValue)] int Quantity
);
