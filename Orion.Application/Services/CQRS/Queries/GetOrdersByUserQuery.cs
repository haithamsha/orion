using Orion.Application.Services.CQRS;
using Orion.Domain.DTOs;

namespace Orion.Application.Services.CQRS.Queries;

/// <summary>
/// Query to get orders by user ID
/// </summary>
public record GetOrdersByUserQuery : IQuery<List<OrderResponse>>
{
    public string UserId { get; init; } = string.Empty;
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 20;

    public GetOrdersByUserQuery() { }

    public GetOrdersByUserQuery(string userId, int skip = 0, int take = 20)
    {
        UserId = userId;
        Skip = skip;
        Take = take;
    }
}