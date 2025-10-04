using Orion.Api.Services.CQRS;
using Orion.Api.Models.DTOs;

namespace Orion.Api.Services.CQRS.Queries;

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