# Orion - Microservices Order Processing System

## 📋 Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Projects Structure](#projects-structure)
- [Technologies](#technologies)
- [Getting Started](#getting-started)
- [API Documentation](#api-documentation)
- [Authentication](#authentication)
- [Background Processing](#background-processing)
- [Real-time Notifications](#real-time-notifications)
- [Database](#database)
- [Testing](#testing)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)

## 🎯 Overview

Orion is a modern microservices-based order processing system built with .NET 8. It demonstrates scalable, asynchronous order processing with real-time notifications and comprehensive testing.

### Key Features
- ⚡ **Fast Asynchronous Processing** - Orders are processed in background workers
- 🔐 **JWT Authentication** - Secure API endpoints with token-based auth
- 🚀 **Real-time Notifications** - SignalR integration for live order status updates
- 📨 **Message Queue Integration** - RabbitMQ for reliable message passing
- 🗄️ **PostgreSQL Database** - Robust data persistence with Entity Framework Core
- 🧪 **Comprehensive Testing** - Unit and integration tests with Moq and XUnit
- 📊 **Background Job Management** - Hangfire for job scheduling and monitoring

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Client Apps   │    │   Orion.Api     │    │  Orion.Worker   │
│  (Web/Mobile)   │◄──►│  (REST API)     │◄──►│ (Background)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │                        │
                              ▼                        ▼
                       ┌─────────────┐         ┌─────────────┐
                       │ PostgreSQL  │         │  RabbitMQ   │
                       │ Database    │         │ Message     │
                       └─────────────┘         │ Queue       │
                                              └─────────────┘
```

### Data Flow
1. **Client** sends order creation request to **API**
2. **API** saves order to **Database** with "Pending" status
3. **API** publishes order event to **RabbitMQ**
4. **API** returns immediate response to **Client**
5. **Worker** consumes message from **RabbitMQ**
6. **Worker** processes order (payment, inventory, etc.)
7. **Worker** updates order status in **Database**
8. **Worker** notifies **API** of completion via HTTP
9. **API** sends real-time update to **Client** via SignalR

## 📁 Projects Structure

```
OrionTr/
├── Orion.sln                    # Solution file
├── Orion.Api/                   # Main REST API project
│   ├── Controllers/             # API endpoints
│   │   ├── AuthController.cs    # JWT authentication
│   │   ├── OrdersController.cs  # Order management
│   │   └── NotificationsController.cs # Real-time notifications
│   ├── Models/                  # Data models
│   │   └── Order.cs            # Order entity
│   ├── Data/                   # Database context
│   │   └── OrionDbContext.cs   # EF Core context
│   ├── Services/               # Business logic
│   │   ├── IMessagePublisher.cs
│   │   ├── RabbitMqPublisher.cs
│   │   ├── IOrderProcessingService.cs
│   │   └── OrderProcessingService.cs
│   ├── Auth/                   # Authentication
│   │   └── ApiKeyAuthFilter.cs # API key validation
│   ├── Hubs/                   # SignalR hubs
│   │   └── OrderStatusHub.cs   # Real-time notifications
│   ├── Migrations/             # Database migrations
│   └── Program.cs              # Application startup
├── Orion.Worker/               # Background worker project
│   ├── OrderProcessorWorker.cs # Main worker service
│   ├── OrderPlacedEvent.cs     # Message contract
│   └── Program.cs              # Worker startup
├── Orion.Api.Tests/            # Test project
│   ├── Unit/                   # Unit tests
│   ├── Integration/            # Integration tests
│   └── AuthControllerTests.cs  # Authentication tests
└── README.md                   # This documentation
```

## 🛠️ Technologies

### Backend
- **Runtime**: .NET 8
- **Framework**: ASP.NET Core 8
- **Database**: PostgreSQL with Entity Framework Core 8
- **Message Queue**: RabbitMQ with RabbitMQ.Client 7.1.2
- **Background Jobs**: Hangfire with PostgreSQL storage
- **Real-time**: SignalR
- **Authentication**: JWT Bearer tokens

### Testing
- **Framework**: XUnit 2.4.2
- **Mocking**: Moq 4.20.72
- **Integration**: Microsoft.AspNetCore.Mvc.Testing 8.0.*
- **In-Memory DB**: Microsoft.EntityFrameworkCore.InMemory 8.0.*

### DevOps
- **Container**: Docker support
- **Version Control**: Git
- **Package Manager**: NuGet

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL 14+
- RabbitMQ Server
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/haithamsha/orion.git
   cd OrionTr
   ```

2. **Set up PostgreSQL Database**
   ```bash
   # Create database
   createdb -U postgres orion_db
   ```

3. **Configure Connection Strings**
   Update `appsettings.json` in both projects:
   ```json
   {
     "ConnectionStrings": {
       "OrionDb": "Host=localhost;Database=orion_db;Username=postgres;Password=yourpassword"
     }
   }
   ```

4. **Install RabbitMQ**
   ```bash
   # On Ubuntu/Debian
   sudo apt install rabbitmq-server
   sudo systemctl start rabbitmq-server
   
   # On macOS
   brew install rabbitmq
   brew services start rabbitmq
   
   # On Windows
   # Download and install from https://www.rabbitmq.com/install-windows.html
   ```

5. **Run Database Migrations**
   ```bash
   cd Orion.Api
   dotnet ef database update
   ```

6. **Start the Applications**
   ```bash
   # Terminal 1 - Start API
   cd Orion.Api
   dotnet run
   
   # Terminal 2 - Start Worker
   cd Orion.Worker
   dotnet run
   ```

### Verify Installation
- API: https://localhost:5001/swagger
- RabbitMQ Management: http://localhost:15672 (guest/guest)

## 📚 API Documentation

### Authentication Endpoints

#### POST /api/auth/login
Authenticate and receive JWT token.

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "testuser",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Order Endpoints

#### POST /api/orders/fast
Create order with background processing.

```http
POST /api/orders/fast
Authorization: Bearer <token>
Content-Type: application/json

{
  "customerName": "John Doe",
  "totalAmount": 99.99
}
```

**Response:**
```json
{
  "id": 1,
  "customerName": "John Doe",
  "totalAmount": 99.99,
  "status": "Pending",
  "createdAt": "2025-10-01T10:00:00Z"
}
```

#### POST /api/orders/slow
Create order with synchronous processing (for comparison).

#### GET /api/orders/{id}
Get order by ID.

#### GET /api/orders
Get all orders.

### Notification Endpoints

#### POST /api/notifications/order-status
Internal endpoint for worker notifications (API Key required).

```http
POST /api/notifications/order-status
X-Api-Key: MySuperSecretWorkerApiKey123!@#
Content-Type: application/json

{
  "orderId": 1,
  "userId": "user123",
  "status": "Completed"
}
```

## 🔐 Authentication

### JWT Token Authentication
- **Endpoint**: `/api/auth/login`
- **Credentials**: `testuser` / `password123`
- **Token Expiry**: 60 minutes
- **Algorithm**: HMAC SHA256

### API Key Authentication
- **Header**: `X-Api-Key`
- **Used For**: Worker → API communication
- **Key**: Configured in `appsettings.json`

### Usage Example
```csharp
// Add JWT token to requests
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

// Add API key to requests
client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
```

## ⚙️ Background Processing

### Order Processing Workflow

1. **Order Created** → Status: `Pending`
2. **Payment Processing** → Status: `Processing` (3s delay)
3. **Inventory Update** → (0.5s delay)
4. **Email Notification** → (2s delay)
5. **Completion** → Status: `Completed` or `Failed`

### Error Handling
- Failed orders are marked with `Failed` status
- Simulated failure: Orders with amount `666`
- All errors are logged with correlation IDs

### Message Queue
- **Exchange**: `order-events` (Fanout)
- **Queue**: `order-processing-queue`
- **Message Format**: JSON
- **Acknowledgment**: Manual after processing

## 🔔 Real-time Notifications

### SignalR Integration
- **Hub**: `/hubs/order-status`
- **Authentication**: JWT required
- **User Targeting**: Based on JWT user ID

### Client Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/order-status", {
        accessTokenFactory: () => localStorage.getItem("token")
    })
    .build();

connection.on("OrderStatusChanged", (orderId, status) => {
    console.log(`Order ${orderId} status: ${status}`);
});
```

## 🗄️ Database

### Schema
```sql
-- Orders table
CREATE TABLE Orders (
    Id SERIAL PRIMARY KEY,
    CustomerName VARCHAR(255) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status INTEGER NOT NULL,
    UserId VARCHAR(255),
    CreatedAt TIMESTAMP NOT NULL
);

-- Order Status Enum
-- 0 = Pending, 1 = Processing, 2 = Completed, 3 = Failed
```

### Migrations
```bash
# Create new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Orion.Api.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories
- **Unit Tests**: Service and controller logic
- **Integration Tests**: End-to-end API testing
- **Authentication Tests**: JWT and API key validation

### Example Test
```csharp
[Fact]
public async Task CreateOrderFast_WhenAuthenticated_ReturnsAccepted()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new CreateOrderRequest("Test Customer", 100);
    
    // Authenticate
    var token = await GetAuthTokenAsync(client);
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    // Act
    var response = await client.PostAsJsonAsync("/api/orders/fast", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
}
```

## ⚙️ Configuration

### API Settings (`Orion.Api/appsettings.json`)
```json
{
  "ConnectionStrings": {
    "OrionDb": "Host=localhost;Database=orion_db;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "https://localhost:5001",
    "Audience": "https://localhost:5001",
    "Key": "ThisIsMySuperSecretKeyForOrionApi12345!"
  },
  "RabbitMq": {
    "HostName": "localhost"
  },
  "ApiKey": "MySuperSecretWorkerApiKey123!@#"
}
```

### Worker Settings (`Orion.Worker/appsettings.json`)
```json
{
  "ConnectionStrings": {
    "OrionDb": "Host=localhost;Database=orion_db;Username=postgres;Password=postgres"
  },
  "RabbitMq": {
    "HostName": "localhost"
  },
  "ApiKey": "MySuperSecretWorkerApiKey123!@#",
  "ApiBaseUrl": "https://localhost:5001"
}
```

### Environment Variables
```bash
# Database
ORION_DB_HOST=localhost
ORION_DB_NAME=orion_db
ORION_DB_USER=postgres
ORION_DB_PASSWORD=postgres

# RabbitMQ
RABBITMQ_HOST=localhost

# Security
JWT_SECRET_KEY=your-secret-key
API_KEY=your-api-key
```

## 🔧 Troubleshooting

### Common Issues

#### Database Connection Failed
```bash
# Check PostgreSQL is running
sudo systemctl status postgresql

# Test connection
psql -h localhost -U postgres -d orion_db
```

#### RabbitMQ Connection Failed
```bash
# Check RabbitMQ is running
sudo systemctl status rabbitmq-server

# Access management UI
open http://localhost:15672
```

#### API Key Authentication Failed
- Verify `ApiKey` matches in both API and Worker `appsettings.json`
- Check `X-Api-Key` header is included in worker requests

#### JWT Token Expired
- Tokens expire after 60 minutes
- Re-authenticate via `/api/auth/login`

#### Worker Cannot Reach API
- Verify `ApiBaseUrl` in worker configuration
- Check API is running on correct port
- Ensure firewall allows connection

### Debugging
```bash
# Enable detailed logging
export ASPNETCORE_ENVIRONMENT=Development

# View SQL queries
"Logging": {
  "LogLevel": {
    "Microsoft.EntityFrameworkCore.Database.Command": "Information"
  }
}
```

## 🤝 Contributing

### Development Workflow
1. Fork the repository
2. Create feature branch: `git checkout -b feature/amazing-feature`
3. Make changes and add tests
4. Run tests: `dotnet test`
5. Commit changes: `git commit -m 'Add amazing feature'`
6. Push to branch: `git push origin feature/amazing-feature`
7. Open Pull Request

### Code Standards
- Follow Microsoft C# coding conventions
- Add XML documentation for public APIs
- Write unit tests for new features
- Update documentation for breaking changes

### Architecture Decisions
- Use async/await for I/O operations
- Implement proper error handling and logging
- Follow SOLID principles
- Use dependency injection for loose coupling

### Pull Request Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Checklist
- [ ] Code follows project standards
- [ ] Self-review completed
- [ ] Documentation updated
```

## 📞 Support

### Getting Help
- **Issues**: [GitHub Issues](https://github.com/haithamsha/orion/issues)
- **Discussions**: [GitHub Discussions](https://github.com/haithamsha/orion/discussions)
- **Email**: haitham@example.com

### Resources
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)

---

**Happy Coding! 🚀**