using Orion.Application.Services.CQRS;
using Orion.Application.Services.CQRS.Commands;
using Orion.Application.DTOs;
using Orion.Infrastructure.Data;
using Orion.Domain.Models;
using Orion.Application.Interfaces;
using Orion.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Orion.Application.Services.CQRS.Handlers;

/// <summary>
/// Command handler for creating orders
/// </summary>
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, OrderResponse>
{
    private readonly OrionDbContext _dbContext;
    private readonly IInventoryService _inventoryService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IEmailService _emailService;
    private readonly IEventStore _eventStore;
    private readonly IProjectionService _projectionService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        OrionDbContext dbContext,
        IInventoryService inventoryService,
        IMessagePublisher messagePublisher,
        IEmailService emailService,
        IEventStore eventStore,
        IProjectionService projectionService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _dbContext = dbContext;
        _inventoryService = inventoryService;
        _messagePublisher = messagePublisher;
        _emailService = emailService;
        _eventStore = eventStore;
        _projectionService = projectionService;
        _logger = logger;
    }

    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing CreateOrderCommand for user {UserId}", request.UserId);

        // STEP 1: Validate inventory items exist
        var productSkus = request.Items.Select(i => i.ProductSku).ToList();
        var inventoryItems = await _dbContext.Inventories
            .Where(i => productSkus.Contains(i.ProductSku))
            .ToListAsync(cancellationToken);

        if (inventoryItems.Count != productSkus.Count)
        {
            var foundSkus = inventoryItems.Select(i => i.ProductSku).ToHashSet();
            var missingSkus = productSkus.Where(sku => !foundSkus.Contains(sku)).ToList();
            throw new InvalidOperationException($"Inventory items not found: {string.Join(", ", missingSkus)}");
        }

        // STEP 2: Reserve inventory using the proper interface
        var reservationRequests = request.Items.Select(item => 
            new InventoryReservationRequest(item.ProductSku, item.Quantity)).ToList();
        
        var reservationResult = await _inventoryService.ReserveInventoryAsync(reservationRequests);
        
        if (!reservationResult.Success)
        {
            throw new InvalidOperationException($"Inventory reservation failed: {reservationResult.Message}");
        }

        // STEP 3: Create Event Sourced Order
        var orderId = Guid.NewGuid();
        var totalAmount = CalculateTotalAmount(request.Items, inventoryItems);
        var orderItemsDetailed = CreateOrderItemsDetailed(request.Items, inventoryItems);

        var orderAggregate = OrderAggregate.CreateNew(
            orderId,
            request.UserId,
            totalAmount,
            orderItemsDetailed,
            notes: $"Order created by {request.CustomerName}",
            triggeredByUserId: request.UserId);

        // STEP 4: Save events to Event Store
        var eventsToProject = orderAggregate.GetUncommittedEvents().ToList();
        await _eventStore.SaveEventsAsync(orderId, eventsToProject, 0);
        orderAggregate.MarkEventsAsCommitted();

        // STEP 4.1: Update read model projections
        await _projectionService.ProjectEventsAsync(eventsToProject, cancellationToken);

        // STEP 5: Create traditional order for compatibility
        var traditionalOrder = await CreateTraditionalOrderAsync(request.UserId, request, inventoryItems, orderId);

        // STEP 6: Confirm inventory reservation
        if (reservationResult.ReservationId.HasValue)
        {
            await _inventoryService.ConfirmReservationAsync(reservationResult.ReservationId.Value);
        }

        // STEP 7: Publish events
        var orderItemsData = request.Items.Select(item =>
        {
            var inventory = inventoryItems.First(inv => inv.ProductSku == item.ProductSku);
            return new OrderItemData(inventory.ProductSku, inventory.ProductName, item.Quantity, inventory.Price);
        }).ToList();

        var orderEvent = new OrderCreatedEvent(
            Guid.Parse(traditionalOrder.Id.ToString()),
            1, // version
            request.UserId,
            request.CustomerName,
            traditionalOrder.TotalAmount,
            orderItemsData);

        await _messagePublisher.PublishOrderPlacedAsync(orderEvent);

        _logger.LogInformation("Order {OrderId} created successfully with Event Sourcing and inventory reserved", orderId);

        // STEP 8: Return order response
        return MapToOrderResponse(traditionalOrder);
    }

    private decimal CalculateTotalAmount(List<OrderItemRequest> items, List<Inventory> inventoryItems)
    {
        decimal total = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var inventory = inventoryItems.First(inv => inv.ProductSku == items[i].ProductSku);
            total += items[i].Quantity * inventory.Price;
        }
        return total;
    }

    private List<OrderItemData> CreateOrderItemsDetailed(List<OrderItemRequest> items, List<Inventory> inventoryItems)
    {
        var orderItems = new List<OrderItemData>();
        for (int i = 0; i < items.Count; i++)
        {
            var requestItem = items[i];
            var inventory = inventoryItems.First(inv => inv.ProductSku == requestItem.ProductSku);
            
            var baseItem = new OrderItemData(
                inventory.ProductSku,
                inventory.ProductName,
                requestItem.Quantity,
                inventory.Price
            );
            
            orderItems.Add(new OrderItemDataDetailed(baseItem, Guid.NewGuid())); // Create new GUID for Event Sourcing ID
        }
        return orderItems;
    }

    private async Task<Order> CreateTraditionalOrderAsync(string userId, CreateOrderCommand request, List<Inventory> inventoryItems, Guid eventSourcedOrderId)
    {
        var order = new Order
        {
            UserId = userId,
            CustomerName = request.CustomerName,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem>()
        };

        decimal totalAmount = 0;

        for (int i = 0; i < request.Items.Count; i++)
        {
            var requestItem = request.Items[i];
            var inventory = inventoryItems.First(inv => inv.ProductSku == requestItem.ProductSku);

            var orderItem = new OrderItem
            {
                Order = order,
                InventoryId = inventory.Id,
                Inventory = inventory,
                ProductName = inventory.ProductName,
                ProductSku = inventory.ProductSku,
                UnitPrice = inventory.Price,
                Quantity = requestItem.Quantity
            };

            order.OrderItems.Add(orderItem);
            totalAmount += orderItem.TotalPrice;
        }

        order.TotalAmount = totalAmount;

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Traditional order {OrderId} created as companion to Event Sourced order {EventSourcedOrderId}", 
            order.Id, eventSourcedOrderId);

        return order;
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