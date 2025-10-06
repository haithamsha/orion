using System.ComponentModel.DataAnnotations;
using Orion.Domain.Models;

namespace Orion.Application.DTOs;

public record ChangeOrderStatusRequest(
    [Required] OrderStatus NewStatus,
    string? Reason = null
);