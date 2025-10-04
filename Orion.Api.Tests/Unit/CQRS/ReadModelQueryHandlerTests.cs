using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Orion.Api.CQRS.QueryHandlers;
using Orion.Api.CQRS.Queries;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.ReadModels;

namespace Orion.Api.Tests.Unit.CQRS;

public class ReadModelQueryHandlerTests : IDisposable
{
    private readonly OrionDbContext _dbContext;
    private readonly Mock<ILogger<GetOrderSummariesQueryHandler>> _summaryLoggerMock;
    private readonly Mock<ILogger<GetOrderDetailQueryHandler>> _detailLoggerMock;
    private readonly Mock<ILogger<GetUserOrderHistoryQueryHandler>> _historyLoggerMock;

    public ReadModelQueryHandlerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<OrionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new OrionDbContext(options);

        // Setup mocks
        _summaryLoggerMock = new Mock<ILogger<GetOrderSummariesQueryHandler>>();
        _detailLoggerMock = new Mock<ILogger<GetOrderDetailQueryHandler>>();
        _historyLoggerMock = new Mock<ILogger<GetUserOrderHistoryQueryHandler>>();

        // Setup test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test order summary views
        _dbContext.OrderSummaryViews.AddRange(
            new OrderSummaryView
            {
                OrderId = 1,
                UserId = "user1",
                CustomerName = "John Doe",
                TotalAmount = 100.00m,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ItemCount = 2,
                StatusDisplay = "Completed",
                FormattedAmount = "$100.00",
                SearchText = "john doe 1 completed",
                IsCompleted = true,
                IsPending = false,
                IsFailed = false
            },
            new OrderSummaryView
            {
                OrderId = 2,
                UserId = "user1",
                CustomerName = "John Doe",
                TotalAmount = 50.00m,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ItemCount = 1,
                StatusDisplay = "Pending",
                FormattedAmount = "$50.00",
                SearchText = "john doe 2 pending",
                IsCompleted = false,
                IsPending = true,
                IsFailed = false
            },
            new OrderSummaryView
            {
                OrderId = 3,
                UserId = "user2",
                CustomerName = "Jane Smith",
                TotalAmount = 75.00m,
                Status = OrderStatus.Failed,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ItemCount = 3,
                StatusDisplay = "Failed",
                FormattedAmount = "$75.00",
                SearchText = "jane smith 3 failed",
                IsCompleted = false,
                IsPending = false,
                IsFailed = true
            }
        );

        // Add test order detail views
        _dbContext.OrderDetailViews.AddRange(
            new OrderDetailView
            {
                OrderId = 1,
                UserId = "user1",
                CustomerName = "John Doe",
                TotalAmount = 100.00m,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                OrderItemsJson = "[{\"ProductSku\":\"TEST-001\",\"Quantity\":2}]",
                StatusHistoryJson = "[{\"Status\":\"Completed\",\"Timestamp\":\"2023-01-01T00:00:00Z\"}]",
                Age = TimeSpan.FromDays(5),
                AgeDisplay = "5 days ago"
            },
            new OrderDetailView
            {
                OrderId = 2,
                UserId = "user1",
                CustomerName = "John Doe",
                TotalAmount = 50.00m,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                OrderItemsJson = "[{\"ProductSku\":\"TEST-002\",\"Quantity\":1}]",
                StatusHistoryJson = "[{\"Status\":\"Pending\",\"Timestamp\":\"2023-01-01T00:00:00Z\"}]",
                Age = TimeSpan.FromDays(2),
                AgeDisplay = "2 days ago"
            }
        );

        // Add test user order history views
        _dbContext.UserOrderHistoryViews.AddRange(
            new UserOrderHistoryView
            {
                Id = "user1_1",
                UserId = "user1",
                OrderId = 1,
                CustomerName = "John Doe",
                TotalAmount = 100.00m,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ItemCount = 2,
                FormattedAmount = "$100.00",
                IsRecent = false
            },
            new UserOrderHistoryView
            {
                Id = "user1_2",
                UserId = "user1",
                OrderId = 2,
                CustomerName = "John Doe",
                TotalAmount = 50.00m,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ItemCount = 1,
                FormattedAmount = "$50.00",
                IsRecent = true
            }
        );

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetOrderSummariesQueryHandler_ReturnsAllOrders_WhenNoFilters()
    {
        // Arrange
        var handler = new GetOrderSummariesQueryHandler(_dbContext, _summaryLoggerMock.Object);
        var query = new GetOrderSummariesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetOrderSummariesQueryHandler_FiltersCorrectly_ByUserId()
    {
        // Arrange
        var handler = new GetOrderSummariesQueryHandler(_dbContext, _summaryLoggerMock.Object);
        var query = new GetOrderSummariesQuery { UserId = "user1" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, item => Assert.Equal("user1", item.UserId));
    }

    [Fact]
    public async Task GetOrderSummariesQueryHandler_FiltersCorrectly_ByStatus()
    {
        // Arrange
        var handler = new GetOrderSummariesQueryHandler(_dbContext, _summaryLoggerMock.Object);
        var query = new GetOrderSummariesQuery { Status = "Completed" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Items.Count());
        Assert.All(result.Items, item => Assert.Equal(OrderStatus.Completed, item.Status));
    }

    [Fact]
    public async Task GetOrderSummariesQueryHandler_FiltersCorrectly_BySearchText()
    {
        // Arrange
        var handler = new GetOrderSummariesQueryHandler(_dbContext, _summaryLoggerMock.Object);
        var query = new GetOrderSummariesQuery { SearchText = "jane" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Items.Count());
        Assert.Contains("jane", result.Items.First().SearchText);
    }

    [Fact]
    public async Task GetOrderSummariesQueryHandler_SortsCorrectly_ByCreatedDate()
    {
        // Arrange
        var handler = new GetOrderSummariesQueryHandler(_dbContext, _summaryLoggerMock.Object);
        var query = new GetOrderSummariesQuery 
        { 
            SortBy = "CreatedAt", 
            SortDescending = true 
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var items = result.Items.ToList();
        Assert.Equal(3, items[0].OrderId); // Most recent (OrderId 3)
        Assert.Equal(2, items[1].OrderId); // Middle (OrderId 2)
        Assert.Equal(1, items[2].OrderId); // Oldest (OrderId 1)
    }

    [Fact]
    public async Task GetOrderSummariesQueryHandler_PaginatesCorrectly()
    {
        // Arrange
        var handler = new GetOrderSummariesQueryHandler(_dbContext, _summaryLoggerMock.Object);
        var query = new GetOrderSummariesQuery 
        { 
            Page = 2, 
            PageSize = 2,
            SortBy = "CreatedAt",
            SortDescending = false 
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.Items.Count()); // Last item on page 2
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task GetOrderDetailQueryHandler_ReturnsOrder_WhenFound()
    {
        // Arrange
        var handler = new GetOrderDetailQueryHandler(_dbContext, _detailLoggerMock.Object);
        var query = new GetOrderDetailQuery(1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.OrderId);
        Assert.Equal("user1", result.UserId);
        Assert.Equal("John Doe", result.CustomerName);
        Assert.Equal(OrderStatus.Completed, result.Status);
    }

    [Fact]
    public async Task GetOrderDetailQueryHandler_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var handler = new GetOrderDetailQueryHandler(_dbContext, _detailLoggerMock.Object);
        var query = new GetOrderDetailQuery(999);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrderDetailQueryHandler_FiltersCorrectly_ByUserId()
    {
        // Arrange
        var handler = new GetOrderDetailQueryHandler(_dbContext, _detailLoggerMock.Object);
        var query = new GetOrderDetailQuery(1, "wronguser");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result); // Should not find order with different user
    }

    [Fact]
    public async Task GetUserOrderHistoryQueryHandler_ReturnsUserOrders()
    {
        // Arrange
        var handler = new GetUserOrderHistoryQueryHandler(_dbContext, _historyLoggerMock.Object);
        var query = new GetUserOrderHistoryQuery("user1");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, item => Assert.Equal("user1", item.UserId));
    }

    [Fact]
    public async Task GetUserOrderHistoryQueryHandler_FiltersCorrectly_ByStatus()
    {
        // Arrange
        var handler = new GetUserOrderHistoryQueryHandler(_dbContext, _historyLoggerMock.Object);
        var query = new GetUserOrderHistoryQuery("user1") { Status = "Completed" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Items.Count());
        Assert.All(result.Items, item => Assert.Equal(OrderStatus.Completed, item.Status));
    }

    [Fact]
    public async Task GetUserOrderHistoryQueryHandler_FiltersCorrectly_ByRecentOnly()
    {
        // Arrange
        var handler = new GetUserOrderHistoryQueryHandler(_dbContext, _historyLoggerMock.Object);
        var query = new GetUserOrderHistoryQuery("user1") { RecentOnly = true };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Items.Count());
        Assert.All(result.Items, item => Assert.True(item.IsRecent));
    }

    [Fact]
    public async Task GetUserOrderHistoryQueryHandler_OrdersCorrectly_ByMostRecent()
    {
        // Arrange
        var handler = new GetUserOrderHistoryQueryHandler(_dbContext, _historyLoggerMock.Object);
        var query = new GetUserOrderHistoryQuery("user1");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var items = result.Items.ToList();
        Assert.Equal(2, items[0].OrderId); // More recent order first
        Assert.Equal(1, items[1].OrderId); // Older order second
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}