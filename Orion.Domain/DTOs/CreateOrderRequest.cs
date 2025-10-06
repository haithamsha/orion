using System.ComponentModel.DataAnnotations;

namespace Orion.Domain.DTOs;


public record CreateOrderRequest(
    [Required] string CustomerName,
    [Required] List<OrderItemRequest> Items
);