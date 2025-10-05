# Orion E-Commerce: Event Sourcing Documentation

This document provides an overview of the Event Sourcing (ES) and Command Query Responsibility Segregation (CQRS) architecture implemented in the Orion E-Commerce backend.

## 1. Core Concepts

### 1.1. Event Sourcing

Instead of storing the current state of our entities (like an `Order`), we store a sequence of immutable events that have happened to that entity. The state of the entity can be rebuilt at any time by replaying these events.

-   **Events**: Immutable facts about what has happened. Examples: `OrderCreatedEvent`, `OrderStatusChangedEvent`. All events inherit from `BaseEvent`.
-   **Aggregates**: An entity that is the root of a consistency boundary. In our case, `OrderAggregate` is the primary aggregate. It processes commands and produces events.
-   **Event Store**: A database that stores all the events. Our `EventStore` service appends events and retrieves them for a given aggregate.

### 1.2. CQRS

CQRS separates the responsibility of handling commands (writes) from handling queries (reads).

-   **Commands**: Represent an intent to change the state of the system. Examples: `CreateOrderCommand`, `ChangeOrderStatusCommand`. They are handled by Command Handlers.
-   **Queries**: Represent a request for data. They do not change the state of the system. Examples: `GetOrderByIdQuery`, `GetOrdersByUserQuery`. They are handled by Query Handlers.
-   **Read Models (Projections)**: Optimized data structures for querying. They are built by listening to events from the event store and updating a separate "read" database. Our `Order` and `OrderItem` EF Core models serve as the primary read models.

## 2. The Flow of a Command

Here's how a typical command flows through the system, using `CreateOrderCommand` as an example:

1.  **API Controller**: The `OrdersController` receives an HTTP request and creates a `CreateOrderCommand`.
2.  **MediatR**: The command is sent to MediatR, which routes it to the appropriate handler (`CreateOrderCommandHandler`).
3.  **Command Handler**:
    a.  Validates the command (e.g., checks inventory).
    b.  Creates a new `OrderAggregate`.
    c.  Calls a method on the aggregate (e.g., `orderAggregate.Create(...)`).
4.  **Aggregate**:
    a.  The `Create` method on the `OrderAggregate` contains the core business logic.
    b.  If the logic succeeds, it creates an `OrderCreatedEvent` and applies it to itself. Applying the event updates the aggregate's internal state.
5.  **Event Store**: The command handler retrieves the uncommitted events from the aggregate and saves them to the `EventStore`.
6.  **Projections**:
    a.  After saving events, the system triggers the `ProjectionService`.
    b.  The `ProjectionService` finds the appropriate handler for the event (e.g., `OrderProjectionHandler` for `OrderCreatedEvent`).
    c.  The `OrderProjectionHandler` updates the read models (the `Orders` and `OrderItems` tables in the PostgreSQL database).

## 3. How to Add a New Feature with ES/CQRS

Let's say we want to add the ability to add a note to an order.

### 3.1. Create a New Command

Create a new command record in the `Orion.Api/Services/CQRS/Commands` folder.

```csharp
// In Orion.Api/Services/CQRS/Commands/AddNoteToOrderCommand.cs
public record AddNoteToOrderCommand(Guid OrderId, string Note, string UserId) : IRequest;
```

### 3.2. Create a New Event

Create a new event class in the `Orion.Api/Models/Events` folder.

```csharp
// In Orion.Api/Models/Events/OrderNoteAddedEvent.cs
public class OrderNoteAddedEvent : BaseEvent
{
    public string Note { get; }

    public OrderNoteAddedEvent(Guid aggregateId, int aggregateVersion, string userId, string note)
        : base(aggregateId, aggregateVersion, userId)
    {
        Note = note;
    }
}
```

### 3.3. Update the Aggregate

Add a new method to `OrderAggregate` to handle the new action and produce the event.

```csharp
// In Orion.Api/Models/OrderAggregate.cs

// ... inside the OrderAggregate class ...

public void AddNote(string note, string userId)
{
    // Business logic/validation can go here
    if (string.IsNullOrWhiteSpace(note))
    {
        throw new ArgumentException("Note cannot be empty.");
    }

    var orderNoteAddedEvent = new OrderNoteAddedEvent(Id, Version + 1, userId, note);
    Apply(orderNoteAddedEvent);
    AddUncommittedEvent(orderNoteAddedEvent);
}

// Also, add a handler for the new event
private void Apply(OrderNoteAddedEvent @event)
{
    // If you were tracking notes in the aggregate's state, you'd update it here.
    // For now, we just increment the version.
    Version = @event.AggregateVersion;
}
```

### 3.4. Create a New Command Handler

Create a new handler in `Orion.Api/Services/CQRS/Handlers`.

```csharp
// In Orion.Api/Services/CQRS/Handlers/AddNoteToOrderCommandHandler.cs
public class AddNoteToOrderCommandHandler : IRequestHandler<AddNoteToOrderCommand>
{
    private readonly IEventStore _eventStore;

    public AddNoteToOrderCommandHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task Handle(AddNoteToOrderCommand request, CancellationToken cancellationToken)
    {
        var events = await _eventStore.GetEventsAsync(request.OrderId);
        var order = new OrderAggregate(request.OrderId, events);

        order.AddNote(request.Note, request.UserId);

        await _eventStore.SaveEventsAsync(order.Id, order.GetUncommittedEvents(), order.Version - order.GetUncommittedEvents().Count());
    }
}
```

### 3.5. Update the Read Model (If Necessary)

If you want to store the note in the `Orders` table, first add the property to the `Order` model.

```csharp
// In Orion.Api/Models/Order.cs
public class Order
{
    // ... other properties
    public string? LastNote { get; set; }
}
```

Then, update the `OrderProjectionHandler` to handle the `OrderNoteAddedEvent`.

```csharp
// In Orion.Api/Projections/OrderProjectionHandler.cs

// ... implement IProjectionHandler<OrderNoteAddedEvent> ...
public async Task HandleAsync(OrderNoteAddedEvent @event, CancellationToken cancellationToken)
{
    var order = await _context.Orders.FirstOrDefaultAsync(o => o.AggregateId == @event.AggregateId);
    if (order != null)
    {
        order.LastNote = @event.Note;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### 3.6. Add an API Endpoint

Finally, add a new endpoint to the `OrdersController`.

```csharp
// In Orion.Api/Controllers/OrdersController.cs
[HttpPost("{id:guid}/notes")]
public async Task<IActionResult> AddNote(Guid id, [FromBody] AddNoteRequest request)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var command = new AddNoteToOrderCommand(id, request.Note, userId);
    await _mediator.Send(command);
    return Accepted();
}
```

This completes the full vertical slice for adding a new feature using the ES/CQRS pattern.
