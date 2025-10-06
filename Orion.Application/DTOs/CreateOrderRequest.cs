using System.ComponentModel.DataAnnotations;

namespace Orion.Application.DTOs;


public record CreateOrderRequest(
    [Required] string CustomerName,
    [Required] List<OrderItemRequest> Items
);