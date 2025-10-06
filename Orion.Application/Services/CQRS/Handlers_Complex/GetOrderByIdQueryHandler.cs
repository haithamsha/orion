using Orion.Application.Services.CQRS;
using Orion.Application.Services.CQRS.Queries;
using Orion.Application.DTOs;
using Orion.Infrastructure.Data;
using Orion.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Orion.Application.Services.CQRS.Handlers;

/// <summary>
/// Query handler for getting order by ID
/// </summary>
public class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderResponse?>
{
    private readonly OrionDbContext _dbContext;
    private readonly ILogger<GetOrderByIdQueryHandler> _logger;

    public GetOrderByIdQueryHandler(
        OrionDbContext dbContext,
        ILogger<GetOrderByIdQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OrderResponse?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing GetOrderByIdQuery for order {OrderId}", request.OrderId);

        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Inventory)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", request.OrderId);
            return null;
        }

        return MapToOrderResponse(order);
    }

    private static OrderResponse MapToOrderResponse(Order order)
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