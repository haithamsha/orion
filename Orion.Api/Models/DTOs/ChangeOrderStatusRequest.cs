using System.ComponentModel.DataAnnotations;
using Orion.Api.Models;

namespace Orion.Api.Models.DTOs;

public record ChangeOrderStatusRequest(
    [Required] OrderStatus NewStatus,
    string? Reason = null
);