┌─────────────────────────────────────────────┐
│              Controllers Layer              │
│  OrdersController │ AuthController │ etc.   │
├─────────────────────────────────────────────┤
│              Business Layer                 │
│ InventoryService │ EmailService │ etc.      │
├─────────────────────────────────────────────┤
│              Data Access Layer              │
│     OrionDbContext │ Repositories          │
├─────────────────────────────────────────────┤
│              Infrastructure Layer           │
│  RabbitMQ │ SMTP │ SignalR │ Logging       │
└─────────────────────────────────────────────┘