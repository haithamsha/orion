using Orion.Api.Services.CQRS;
using Orion.Api.ReadModels;

namespace Orion.Api.CQRS.Queries;

/// <summary>
/// Query to get optimized order summaries with filtering and paging
/// </summary>
public record GetOrderSummariesQuery : IQuery<PagedResult<OrderSummaryView>>
{
    public string? UserId { get; init; }
    public string? SearchText { get; init; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "CreatedAt";
    public bool SortDescending { get; init; } = true;
}

/// <summary>
/// Query to get detailed order view by ID
/// </summary>
public record GetOrderDetailQuery : IQuery<OrderDetailView?>
{
    public int OrderId { get; init; }
    public string? UserId { get; init; } // For security filtering

    public GetOrderDetailQuery(int orderId, string? userId = null)
    {
        OrderId = orderId;
        UserId = userId;
    }
}

/// <summary>
/// Query to get user's order history efficiently
/// </summary>
public record GetUserOrderHistoryQuery : IQuery<PagedResult<UserOrderHistoryView>>
{
    public string UserId { get; init; } = string.Empty;
    public string? Status { get; init; }
    public bool RecentOnly { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public GetUserOrderHistoryQuery(string userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Paged result wrapper
/// </summary>
public record PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}