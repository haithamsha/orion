┌─────────────────────────────────────────────────────────────┐ │ Client Layer │ ├─────────────────────────────────────────────────────────────┤ │ Web Apps │ Mobile Apps │ Admin Panel │ Third-Party │ └─────────────┬───────────────────────────────────────────────┘ │ HTTPS/REST/WebSocket ┌─────────────▼───────────────────────────────────────────────┐ │ API Gateway Layer │ ├─────────────────────────────────────────────────────────────┤ │ Authentication │ Rate Limiting │ Load Balancing │ └─────────────┬───────────────────────────────────────────────┘ │ ┌─────────────▼───────────────────────────────────────────────┐ │ Application Services │ ├─────────────────────────────────────────────────────────────┤ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐ │ │ │ Orion.Api │ │Orion.Worker │ │ SignalR Hub │ │ │ │ (REST API) │ │(Background) │ │ (Real-time Comms) │ │ │ │ │ │ Processor │ │ │ │ │ └─────────────┘ └─────────────┘ └─────────────────────────┘ │ └─────────────┬───────────────────────────────────────────────┘ │ Event Bus (RabbitMQ) ┌─────────────▼───────────────────────────────────────────────┐ │ Infrastructure Layer │ ├─────────────────────────────────────────────────────────────┤ │ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐ │ │ │ PostgreSQL │ │ RabbitMQ │ │ SMTP Service │ │ │ │ Database │ │ Message │ │ Email Delivery │ │ │ │ │ │ Broker │ │ │ │ │ └─────────────┘ └─────────────┘ └─────────────────────────┘ │ └─────────────────────────────────────────────────────────────┘


### Architecture Principles

#### 1. **Event-Driven Architecture (EDA)**
- **Loose Coupling**: Services communicate through events, not direct calls
- **Scalability**: Each service can scale independently
- **Resilience**: Failure in one service doesn't cascade to others
- **Auditability**: All business events are recorded for analysis

#### 2. **Domain-Driven Design (DDD)**
- **Bounded Contexts**: Clear service boundaries around business domains
- **Ubiquitous Language**: Consistent terminology across code and documentation
- **Aggregate Roots**: Order and Inventory as primary business entities
- **Value Objects**: Immutable data structures for business concepts

#### 3. **CQRS (Command Query Responsibility Segregation)**
- **Command Side**: Order creation, inventory updates (write operations)
- **Query Side**: Order retrieval, inventory lookup (read operations)
- **Separation**: Different models optimized for different purposes

#### 4. **Saga Pattern**
- **Distributed Transactions**: Multi-step business processes across services
- **Compensation**: Automatic rollback on failures
- **State Management**: Order processing states tracked explicitly

## Service Architecture

### Orion.Api Service

#### Responsibilities
- **HTTP API Endpoints**: RESTful interfaces for client applications
- **Authentication & Authorization**: JWT token validation and user context
- **Input Validation**: Request data validation and sanitization
- **Business Orchestration**: Coordinating business operations
- **Real-time Communication**: SignalR hub management

#### Technical Stack
```csharp
// Core Framework
.NET 8.0
ASP.NET Core Web API
Entity Framework Core 8.0

// Authentication
Microsoft.AspNetCore.Authentication.JwtBearer
System.IdentityModel.Tokens.Jwt

// Real-time Communication
Microsoft.AspNetCore.SignalR

// Database
Npgsql.EntityFrameworkCore.PostgreSQL

// Message Broker
RabbitMQ.Client

// Email Services
System.Net.Mail (SMTP)