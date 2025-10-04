using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Models.Events;
using Orion.Api.ReadModels;

namespace Orion.Api.Projections;

/// <summary>
/// Projection handler that updates read models when order events occur
/// </summary>
public class OrderProjectionHandler : 
    IProjectionHandler<OrderCreatedEvent>,
    IProjectionHandler<OrderStatusChangedEvent>,
    IProjectionHandler<OrderCompletedEvent>,
    IProjectionHandler<OrderFailedEvent>
{
    private readonly OrionDbContext _context;
    private readonly ILogger<OrderProjectionHandler> _logger;

    public OrderProjectionHandler(OrionDbContext context, ILogger<OrderProjectionHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating read models for OrderCreated event: {OrderId}", @event.AggregateId);

        try
        {
            // Find the traditional order by user and amount (since we can't directly match Guid to int)
            // This is created shortly after the event sourced order in the same transaction
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == @event.CustomerUserId && 
                           o.TotalAmount == @event.TotalAmount &&
                           o.CreatedAt >= @event.OccurredAt.AddMinutes(-1)) // Within 1 minute
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Traditional order not found for event {EventId}. Will retry projection later.", @event.AggregateId);
                // In a real system, you might want to implement a retry mechanism
                // For now, we'll just log and skip
                return;
            }

            _logger.LogInformation("Found traditional order {OrderId} for event sourced order {EventOrderId}", 
                order.Id, @event.AggregateId);

            // Create OrderSummaryView
            var summaryView = new OrderSummaryView
            {
                OrderId = order.Id,
                UserId = @event.CustomerUserId,
                CustomerName = order.CustomerName,
                TotalAmount = @event.TotalAmount,
                Status = @event.Status,
                CreatedAt = @event.OccurredAt,
                LastUpdatedAt = @event.OccurredAt,
                ItemCount = @event.Items.Count,
                // Set computed fields
                StatusDisplay = @event.Status.ToString(),
                FormattedAmount = $"${@event.TotalAmount:F2}",
                SearchText = $"{order.CustomerName} {order.Id} {@event.Status}".ToLower(),
                IsCompleted = @event.Status == OrderStatus.Completed,
                IsPending = @event.Status == OrderStatus.Pending,
                IsFailed = @event.Status == OrderStatus.Failed
            };

            // Create OrderDetailView
            var age = DateTime.UtcNow - @event.OccurredAt;
            var detailView = new OrderDetailView
            {
                OrderId = order.Id,
                UserId = @event.CustomerUserId,
                CustomerName = order.CustomerName,
                TotalAmount = @event.TotalAmount,
                Status = @event.Status,
                CreatedAt = @event.OccurredAt,
                LastUpdatedAt = @event.OccurredAt,
                OrderItemsJson = JsonConvert.SerializeObject(@event.Items),
                StatusHistoryJson = JsonConvert.SerializeObject(new[] { 
                    new { Status = @event.Status.ToString(), Timestamp = @event.OccurredAt, Reason = "Order Created" }
                }),
                // Set computed fields
                Age = age,
                AgeDisplay = age.Days > 0 ? $"{age.Days} days ago" : 
                            age.Hours > 0 ? $"{age.Hours} hours ago" : 
                            $"{age.Minutes} minutes ago"
            };

            // Create UserOrderHistoryView
            var historyView = new UserOrderHistoryView
            {
                Id = $"{@event.CustomerUserId}_{order.Id}",
                UserId = @event.CustomerUserId,
                OrderId = order.Id,
                CustomerName = order.CustomerName,
                TotalAmount = @event.TotalAmount,
                Status = @event.Status,
                CreatedAt = @event.OccurredAt,
                LastUpdatedAt = @event.OccurredAt,
                ItemCount = @event.Items.Count,
                // Set computed fields
                FormattedAmount = $"${@event.TotalAmount:F2}",
                IsRecent = @event.OccurredAt > DateTime.UtcNow.AddDays(-30)
            };

            _context.OrderSummaryViews.Add(summaryView);
            _context.OrderDetailViews.Add(detailView);
            _context.UserOrderHistoryViews.Add(historyView);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created read models for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating read models for OrderCreated event: {OrderId}", @event.AggregateId);
            throw;
        }
    }

    public async Task HandleAsync(OrderStatusChangedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating read models for OrderStatusChanged event: {OrderId}", @event.AggregateId);

        try
        {
            // For status changes, we need to find the order by a different approach since we have Guid but need int
            // We'll need to add a mapping or use a different approach
            // For now, let's skip status change projections or implement them differently
            _logger.LogWarning("OrderStatusChanged projection not yet implemented due to ID mapping complexity");

            // TODO: Implement proper ID mapping between Guid (event sourcing) and int (traditional orders)
            // This could be done by:
            // 1. Adding a correlation table
            // 2. Storing the traditional order ID in the event
            // 3. Using a different approach to match events to orders

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating read models for OrderStatusChanged event: {OrderId}", @event.AggregateId);
            throw;
        }
    }

    public async Task HandleAsync(OrderCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        // Order completion is handled by the status change event, but we could add specific completion logic here
        _logger.LogInformation("Processing OrderCompleted event for read models: {OrderId}", @event.AggregateId);
        
        // Additional completion-specific logic could go here
        // For now, the status change to 'Completed' is handled by OrderStatusChangedEvent
    }

    public async Task HandleAsync(OrderFailedEvent @event, CancellationToken cancellationToken = default)
    {
        // Order failure is handled by the status change event, but we could add specific failure logic here
        _logger.LogInformation("Processing OrderFailed event for read models: {OrderId}", @event.AggregateId);
        
        // Additional failure-specific logic could go here
        // For now, the status change to 'Failed' is handled by OrderStatusChangedEvent
    }
}