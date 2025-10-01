using System.ComponentModel.DataAnnotations;

namespace Orion.Api.Models.DTOs;

public record OrderItemRequest(
    [Required] string ProductSku,
    [Range(1, int.MaxValue)] int Quantity
);
