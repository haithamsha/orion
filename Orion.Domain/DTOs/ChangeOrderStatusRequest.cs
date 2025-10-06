using System.ComponentModel.DataAnnotations;
using Orion.Domain.Models;

namespace Orion.Domain.DTOs;

public record ChangeOrderStatusRequest(
    [Required] OrderStatus NewStatus,
    string? Reason = null
);