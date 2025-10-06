using Orion.Application.Services.CQRS;
using Orion.Domain.DTOs;

namespace Orion.Application.Services.CQRS.Queries;

/// <summary>
/// Query to get order by ID
/// </summary>
public record GetOrderByIdQuery : IQuery<OrderResponse?>
{
    public int OrderId { get; init; }

    public GetOrderByIdQuery() { }

    public GetOrderByIdQuery(int orderId)
    {
        OrderId = orderId;
    }
}