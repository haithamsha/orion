using Orion.Application.Services.CQRS;
using Orion.Domain.Models;

namespace Orion.Application.Services.CQRS.Commands;

/// <summary>
/// Command to change order status
/// </summary>
public record ChangeOrderStatusCommand : ICommand
{
    public int OrderId { get; init; }
    public OrderStatus NewStatus { get; init; }
    public string? Reason { get; init; }

    public ChangeOrderStatusCommand() { }

    public ChangeOrderStatusCommand(int orderId, OrderStatus newStatus, string? reason = null)
    {
        OrderId = orderId;
        NewStatus = newStatus;
        Reason = reason;
    }
}