using System.ComponentModel.DataAnnotations;

namespace Orion.Api.Models.DTOs;


public record CreateOrderRequest(
    [Required] string CustomerName,
    [Required] List<OrderItemRequest> Items
);