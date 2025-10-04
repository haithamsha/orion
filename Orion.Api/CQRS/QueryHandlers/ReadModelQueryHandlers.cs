using Microsoft.EntityFrameworkCore;
using Orion.Api.Services.CQRS;
using Orion.Api.CQRS.Queries;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.ReadModels;

namespace Orion.Api.CQRS.QueryHandlers;

/// <summary>
/// Handler for optimized order summary queries
/// </summary>
public class GetOrderSummariesQueryHandler : IQueryHandler<GetOrderSummariesQuery, PagedResult<OrderSummaryView>>
{
    private readonly OrionDbContext _context;
    private readonly ILogger<GetOrderSummariesQueryHandler> _logger;

    public GetOrderSummariesQueryHandler(OrionDbContext context, ILogger<GetOrderSummariesQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<OrderSummaryView>> Handle(GetOrderSummariesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing GetOrderSummariesQuery with filters: UserId={UserId}, Page={Page}", 
            request.UserId, request.Page);

        var query = _context.OrderSummaryViews.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.UserId))
        {
            query = query.Where(o => o.UserId == request.UserId);
        }

        if (!string.IsNullOrEmpty(request.SearchText))
        {
            query = query.Where(o => o.SearchText.Contains(request.SearchText.ToLower()));
        }

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<OrderStatus>(request.Status, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
        }

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "createdat" => request.SortDescending 
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt),
            "totalamount" => request.SortDescending 
                ? query.OrderByDescending(o => o.TotalAmount)
                : query.OrderBy(o => o.TotalAmount),
            "customername" => request.SortDescending 
                ? query.OrderByDescending(o => o.CustomerName)
                : query.OrderBy(o => o.CustomerName),
            "status" => request.SortDescending 
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            _ => request.SortDescending 
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt)
        };

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} order summaries out of {Total} total", 
            items.Count, totalCount);

        return new PagedResult<OrderSummaryView>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

/// <summary>
/// Handler for order detail queries
/// </summary>
public class GetOrderDetailQueryHandler : IQueryHandler<GetOrderDetailQuery, OrderDetailView?>
{
    private readonly OrionDbContext _context;
    private readonly ILogger<GetOrderDetailQueryHandler> _logger;

    public GetOrderDetailQueryHandler(OrionDbContext context, ILogger<GetOrderDetailQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrderDetailView?> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing GetOrderDetailQuery for OrderId={OrderId}, UserId={UserId}", 
            request.OrderId, request.UserId);

        var query = _context.OrderDetailViews
            .Where(o => o.OrderId == request.OrderId);

        // Apply user filter for security if provided
        if (!string.IsNullOrEmpty(request.UserId))
        {
            query = query.Where(o => o.UserId == request.UserId);
        }

        var result = await query.FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("Order detail not found for OrderId={OrderId}, UserId={UserId}", 
                request.OrderId, request.UserId);
        }

        return result;
    }
}

/// <summary>
/// Handler for user order history queries
/// </summary>
public class GetUserOrderHistoryQueryHandler : IQueryHandler<GetUserOrderHistoryQuery, PagedResult<UserOrderHistoryView>>
{
    private readonly OrionDbContext _context;
    private readonly ILogger<GetUserOrderHistoryQueryHandler> _logger;

    public GetUserOrderHistoryQueryHandler(OrionDbContext context, ILogger<GetUserOrderHistoryQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<UserOrderHistoryView>> Handle(GetUserOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing GetUserOrderHistoryQuery for UserId={UserId}, Page={Page}", 
            request.UserId, request.Page);

        var query = _context.UserOrderHistoryViews
            .Where(o => o.UserId == request.UserId);

        // Apply filters
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<OrderStatus>(request.Status, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (request.RecentOnly)
        {
            query = query.Where(o => o.IsRecent);
        }

        // Order by most recent first
        query = query.OrderByDescending(o => o.CreatedAt);

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} user order history items out of {Total} total for user {UserId}", 
            items.Count, totalCount, request.UserId);

        return new PagedResult<UserOrderHistoryView>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}