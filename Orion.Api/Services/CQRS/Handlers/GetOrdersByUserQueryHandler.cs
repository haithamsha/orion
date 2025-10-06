using Orion.Api.Services.CQRS;
using Orion.Api.Services.CQRS.Queries;
using Orion.Api.Models.DTOs;
using Orion.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Orion.Api.Services.CQRS.Handlers;

/// <summary>
/// Query handler for getting orders by user ID
/// </summary>
public class GetOrdersByUserQueryHandler : IQueryHandler<GetOrdersByUserQuery, List<OrderResponse>>
{
    private readonly OrionDbContext _dbContext;
    private readonly ILogger<GetOrdersByUserQueryHandler> _logger;

    public GetOrdersByUserQueryHandler(
        OrionDbContext dbContext,
        ILogger<GetOrdersByUserQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<OrderResponse>> Handle(GetOrdersByUserQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing GetOrdersByUserQuery for user {UserId} (Skip: {Skip}, Take: {Take})", 
            request.UserId, request.Skip, request.Take);

        var orders = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Inventory)
            .Where(o => o.UserId == request.UserId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} orders for user {UserId}", orders.Count, request.UserId);

        return orders.Select(MapToOrderResponse).ToList();
    }

    private static OrderResponse MapToOrderResponse(Models.Order order)
    {
        return new OrderResponse(
            order.Id,
            order.UserId,
            order.CustomerName,
            order.TotalAmount,
            order.Status.ToString(),
            order.CreatedAt,
            order.OrderItems.Select(oi => new OrderItemResponse(
                oi.ProductName,
                oi.ProductSku,
                oi.UnitPrice,
                oi.Quantity,
                oi.TotalPrice
            )).ToList()
        );
    }
}