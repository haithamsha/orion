using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Orion.Api.Services.CQRS.Handlers;
using Orion.Api.Services.CQRS.Commands;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Services;
using Orion.Api.Services.EventSourcing;
using Orion.Api.Projections;
using Orion.Api.Models.DTOs;
using Orion.Api.Models.Events;
using EventSourcingStore = Orion.Api.Services.EventSourcing.IEventStore;

namespace Orion.Api.Tests.Unit.CQRS;

public class CreateOrderCommandHandlerTests : IDisposable
{
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<IMessagePublisher> _messagePublisherMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<EventSourcingStore> _eventStoreMock;
    private readonly Mock<IProjectionService> _projectionServiceMock;
    private readonly Mock<ILogger<CreateOrderCommandHandler>> _loggerMock;
    private readonly OrionDbContext _dbContext;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<OrionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new OrionDbContext(options);

        // Setup mocks
        _inventoryServiceMock = new Mock<IInventoryService>();
        _messagePublisherMock = new Mock<IMessagePublisher>();
        _emailServiceMock = new Mock<IEmailService>();
        _eventStoreMock = new Mock<EventSourcingStore>();
        _projectionServiceMock = new Mock<IProjectionService>();
        _loggerMock = new Mock<ILogger<CreateOrderCommandHandler>>();

        // Create handler
        _handler = new CreateOrderCommandHandler(
            _dbContext,
            _inventoryServiceMock.Object,
            _messagePublisherMock.Object,
            _emailServiceMock.Object,
            _eventStoreMock.Object,
            _projectionServiceMock.Object,
            _loggerMock.Object);

        // Setup test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test inventory items
        _dbContext.Inventories.AddRange(
            new Inventory { Id = 1, ProductSku = "TEST-001", ProductName = "Test Product 1", Price = 10.00m, AvailableQuantity = 100 },
            new Inventory { Id = 2, ProductSku = "TEST-002", ProductName = "Test Product 2", Price = 20.00m, AvailableQuantity = 50 }
        );
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrderSuccessfully()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            UserId = "test-user-123",
            CustomerName = "Test Customer",
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest("TEST-001", 2),
                new OrderItemRequest("TEST-002", 1)
            }
        };

        var reservationResult = new InventoryReservationResult(true, "Success", 123);
        _inventoryServiceMock
            .Setup(x => x.ReserveInventoryAsync(It.IsAny<List<InventoryReservationRequest>>()))
            .ReturnsAsync(reservationResult);

        _inventoryServiceMock
            .Setup(x => x.ConfirmReservationAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        _eventStoreMock
            .Setup(x => x.SaveEventsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<BaseEvent>>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _projectionServiceMock
            .Setup(x => x.ProjectEventsAsync(It.IsAny<IEnumerable<BaseEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _messagePublisherMock
            .Setup(x => x.PublishOrderPlacedAsync(It.IsAny<OrderPlacedEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-user-123", result.UserId);
        Assert.Equal("Test Customer", result.CustomerName);
        Assert.Equal(40.00m, result.TotalAmount); // (2 * 10.00) + (1 * 20.00)
        Assert.Equal(2, result.Items.Count);

        // Verify inventory reservation was called
        _inventoryServiceMock.Verify(x => x.ReserveInventoryAsync(It.IsAny<List<InventoryReservationRequest>>()), Times.Once);
        _inventoryServiceMock.Verify(x => x.ConfirmReservationAsync(reservationResult.ReservationId!.Value), Times.Once);

        // Verify event store was called
        _eventStoreMock.Verify(x => x.SaveEventsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<BaseEvent>>(), 0), Times.Once);

        // Verify projections were updated
        _projectionServiceMock.Verify(x => x.ProjectEventsAsync(It.IsAny<IEnumerable<BaseEvent>>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify message was published
        _messagePublisherMock.Verify(x => x.PublishOrderPlacedAsync(It.IsAny<OrderPlacedEvent>()), Times.Once);

        // Verify order was saved to database
        var savedOrder = await _dbContext.Orders.FirstAsync();
        Assert.Equal("test-user-123", savedOrder.UserId);
        Assert.Equal("Test Customer", savedOrder.CustomerName);
        Assert.Equal(OrderStatus.Pending, savedOrder.Status);
    }

    [Fact]
    public async Task Handle_InvalidInventoryItems_ThrowsException()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            UserId = "test-user-123",
            CustomerName = "Test Customer",
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest("INVALID-SKU", 1)
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(command, CancellationToken.None));
        
        Assert.Contains("Inventory items not found", exception.Message);
        Assert.Contains("INVALID-SKU", exception.Message);
    }

    [Fact]
    public async Task Handle_InventoryReservationFails_ThrowsException()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            UserId = "test-user-123",
            CustomerName = "Test Customer",
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest("TEST-001", 2)
            }
        };

        var failedReservationResult = new InventoryReservationResult(false, "Insufficient stock", null);
        _inventoryServiceMock
            .Setup(x => x.ReserveInventoryAsync(It.IsAny<List<InventoryReservationRequest>>()))
            .ReturnsAsync(failedReservationResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(command, CancellationToken.None));
        
        Assert.Contains("Inventory reservation failed", exception.Message);
        Assert.Contains("Insufficient stock", exception.Message);

        // Verify no events were saved
        _eventStoreMock.Verify(x => x.SaveEventsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<BaseEvent>>(), It.IsAny<int>()), Times.Never);
        
        // Verify no order was saved to database
        var orderCount = await _dbContext.Orders.CountAsync();
        Assert.Equal(0, orderCount);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCorrectOrderItems()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            UserId = "test-user-123",
            CustomerName = "Test Customer",
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest("TEST-001", 3),
                new OrderItemRequest("TEST-002", 2)
            }
        };

        var reservationResult = new InventoryReservationResult(true, "Success", 456);
        _inventoryServiceMock
            .Setup(x => x.ReserveInventoryAsync(It.IsAny<List<InventoryReservationRequest>>()))
            .ReturnsAsync(reservationResult);

        _inventoryServiceMock
            .Setup(x => x.ConfirmReservationAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        _eventStoreMock
            .Setup(x => x.SaveEventsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<BaseEvent>>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _projectionServiceMock
            .Setup(x => x.ProjectEventsAsync(It.IsAny<IEnumerable<BaseEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _messagePublisherMock
            .Setup(x => x.PublishOrderPlacedAsync(It.IsAny<OrderPlacedEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Items.Count);
        
        var item1 = result.Items.First(i => i.ProductSku == "TEST-001");
        Assert.Equal("Test Product 1", item1.ProductName);
        Assert.Equal(3, item1.Quantity);
        Assert.Equal(10.00m, item1.UnitPrice);
        Assert.Equal(30.00m, item1.TotalPrice);

        var item2 = result.Items.First(i => i.ProductSku == "TEST-002");
        Assert.Equal("Test Product 2", item2.ProductName);
        Assert.Equal(2, item2.Quantity);
        Assert.Equal(20.00m, item2.UnitPrice);
        Assert.Equal(40.00m, item2.TotalPrice);

        Assert.Equal(70.00m, result.TotalAmount); // 30.00 + 40.00
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}