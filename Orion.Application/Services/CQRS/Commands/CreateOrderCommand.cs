using Orion.Application.Services.CQRS;
using Orion.Application.DTOs;

namespace Orion.Application.Services.CQRS.Commands;

/// <summary>
/// Command to create a new order
/// </summary>
public record CreateOrderCommand : ICommand<OrderResponse>
{
    public string UserId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public List<OrderItemRequest> Items { get; init; } = new();

    public CreateOrderCommand() { }

    public CreateOrderCommand(string userId, string customerName, List<OrderItemRequest> items)
    {
        UserId = userId;
        CustomerName = customerName;
        Items = items;
    }
}