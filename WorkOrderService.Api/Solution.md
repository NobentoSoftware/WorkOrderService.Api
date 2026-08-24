# Solution Design

## Overview

This solution implements a small backend service for managing work orders and asynchronously processing external progress events.

The goal was to keep the implementation focused while still demonstrating production-minded design decisions around persistence, API contracts, background processing, idempotency, database modelling, and automated testing.

The main technologies used are:

* .NET 8
* ASP.NET Core Minimal APIs
* Entity Framework Core
* SQL Server
* `Channel<T>` for in-memory background event buffering
* `BackgroundService` for asynchronous processing
* xUnit
* SQLite in-memory for integration testing

## Architecture

The application is intentionally kept as a small single API project rather than introducing a multi-project Clean Architecture structure.

The main areas are separated by folders:

```text
Domain
Contracts
Endpoints
Persistence
BackgroundProcessing
```

This provides enough separation for maintainability without introducing unnecessary abstractions for the size of the assignment.

Minimal API endpoint definitions are placed in endpoint extension classes instead of keeping every route directly in `Program.cs`.

This keeps `Program.cs` focused primarily on application configuration and dependency registration.

## Work Order Model

The main entity is `WorkOrder`.

A work order contains information such as:

```text
Id
ExternalId
SiteCode
Description
Status
CreatedAt
UpdatedAt
```

`ExternalId` is unique because it represents the identifier used to reference a work order externally.

The work order status is represented using an enum.

The status is stored as a string in the database rather than an integer. This makes database records easier to inspect and avoids relying on enum numeric ordering.

## Status History

Status history is stored in a separate relational table rather than embedding history inside the work order.

The relationship is:

```text
WorkOrder
    1
    |
    *
WorkOrderStatusHistory
```

Each history record contains:

```text
WorkOrderId
FromStatus
ToStatus
ChangedAt
```

Whenever a work order's status changes, a corresponding history record is created.

The single work-order GET endpoint includes this status history in its response.

## API Contracts

EF Core entities are not used directly as the main API response shape.

Separate response DTOs are used where appropriate.

This avoids exposing persistence implementation details and prevents issues caused by bidirectional EF Core navigation properties.

For example:

```text
WorkOrder
    -> StatusHistory
        -> WorkOrder
            -> StatusHistory
```

Returning this entity graph directly can cause circular JSON serialization.

Using API response DTOs also provides better control over the public contract.

## Work Order Endpoints

The service exposes endpoints for:

```text
POST   /api/work-orders
GET    /api/work-orders/{id}
PATCH  /api/work-orders/{id}/status
GET    /api/work-orders
```

The list endpoint supports an optional status filter.

A fixed page size is used to stay within the requested assignment scope.

## Background Processing

Progress events are accepted through:

```http
POST /api/progress-events
```

The HTTP endpoint does not directly perform the work-order update.

Instead, the flow is:

```text
HTTP request
    |
    v
ProgressEventQueue
    |
    v
Channel<T>
    |
    v
ProgressEventWorker
    |
    v
EF Core / SQL Server
```

The endpoint queues the event and returns:

```text
202 Accepted
```

This reflects that the request has been accepted for asynchronous processing but has not necessarily completed when the HTTP response is returned.

## In-Memory Queue

The queue uses a bounded `Channel<T>`.

A bounded channel was selected because it is thread-safe, designed for producer/consumer workloads, and integrates well with asynchronous .NET code.

The queue has a fixed capacity.

This avoids allowing unlimited events to accumulate in application memory.

If the buffer becomes full, producers wait for capacity rather than allowing uncontrolled memory growth.

## Hosted Background Service

A `BackgroundService` continuously reads events from the queue.

For each event, the worker:

1. Creates a dependency injection scope.
2. Resolves `AppDbContext`.
3. Checks whether the event was already processed.
4. Looks up the referenced work order.
5. Validates the requested status.
6. Updates the work order.
7. Adds status history.
8. Stores the event as processed.
9. Commits the changes.

The worker uses `IServiceScopeFactory` instead of injecting `AppDbContext` directly.

`BackgroundService` is effectively long-lived, while `DbContext` is registered as a scoped service. Creating a scope for processing allows each operation to use an appropriately scoped EF Core context.

## Event De-duplication

The requirements allow event de-duplication to be stored only in memory.

I chose to persist processed event identifiers instead.

The application contains a `ProcessedProgressEvent` table containing information such as:

```text
Id
EventId
WorkOrderId
ProcessedAt
```

A unique index is placed on:

```text
EventId
```

Before processing an event, the worker checks whether the event has already been processed.

If it has, the event is ignored.

This provides durable de-duplication across application restarts.

### Why Persistence Was Chosen

An in-memory `HashSet<string>` would have been simpler, but it has important limitations:

* de-duplication state disappears when the application restarts
* it would not naturally support multiple application instances
* it provides no historical evidence of processed events

Persisting processed IDs adds little complexity because SQL Server and EF Core are already part of the application.

The database unique constraint also provides additional protection against accidental duplicate records.

## Transaction Considerations

The work-order update, status history insertion, and processed-event insertion are performed within the same EF Core `SaveChangesAsync` operation.

This means EF Core executes them transactionally for the normal relational database operation.

This reduces the chance that a work order status is updated without the corresponding history and processed-event record being persisted.

For a larger production system, additional concurrency protection and retry strategies would be considered.

## Error Handling

The service uses standard HTTP response semantics where possible.

Examples include:

```text
201 Created
202 Accepted
400 Bad Request
404 Not Found
409 Conflict
```

Background processing failures are logged rather than being returned to the original HTTP client, because the HTTP request has already completed by the time the worker processes the event.

A production system would likely introduce more structured failure handling, dead-letter processing, and observability.

## Persistence

The main application uses EF Core with SQL Server.

At least one EF Core migration is included in the repository.

The database contains relational structures for:

```text
WorkOrders
WorkOrderStatusHistories
ProcessedProgressEvents
```

Foreign keys and unique indexes are configured through EF Core.

## Testing Strategy

The project contains both unit and integration testing.

### Unit Tests

The unit tests focus on business behaviour rather than framework implementation details.

This keeps the tests small and fast.

### Integration Test

The integration test uses:

```text
WebApplicationFactory<Program>
```

to host the ASP.NET Core application inside the test process.

The test sends an actual HTTP request to:

```http
POST /api/work-orders
```

and verifies that the endpoint returns the expected result.

This tests several components together:

```text
HTTP routing
Minimal API endpoint
Dependency injection
EF Core
Database persistence
JSON serialization
HTTP response
```

## SQLite for Integration Testing

The production application uses SQL Server.

The integration test replaces SQL Server with an SQLite in-memory database.

This was chosen because it:

* is lightweight
* requires no external database during tests
* provides relational behaviour
* makes the test suite easy to run locally and in CI

The SQLite connection is kept open for the lifetime of the test server because an SQLite in-memory database exists only while its connection remains open.

Using SQLite instead of SQL Server means the integration tests do not validate every SQL Server-specific behaviour.

For this assignment, that trade-off was considered acceptable and is explicitly allowed by the requirements.

For a production system, an additional integration-test suite using a real SQL Server container could be added.

## Swagger

Swagger/OpenAPI support was added to simplify development and demonstration of the API.

It allows the work-order endpoints and progress-event endpoint to be exercised directly from the browser.

## Trade-offs

### Minimal API Structure

Minimal APIs were required by the assignment.

Endpoint mappings are separated into endpoint classes to prevent `Program.cs` from growing excessively.

I did not introduce controllers because they were unnecessary for the requested architecture.

### No Repository Layer

A repository abstraction was deliberately not introduced.

EF Core already provides much of the functionality that a generic repository would wrap.

For the limited size of this service, directly using `AppDbContext` reduces unnecessary indirection.

If the business logic became significantly more complex, application services or more focused persistence abstractions could be introduced.

### No MediatR

MediatR or CQRS-style handlers were not added.

Although they can be useful in larger applications, adding them here would increase complexity without providing enough benefit for the size of the assignment.

### In-Memory Messaging

The event queue exists only in application memory.

This is appropriate for the requested assignment scope but is not durable.

If the process stops after returning `202 Accepted` but before processing the event, that queued event will be lost.

## Production Improvements

If this service were being developed for a full production environment, I would consider the following improvements.

### Durable Messaging

Replace the in-memory channel with a durable messaging service such as:

```text
Azure Service Bus
RabbitMQ
Amazon SQS
Kafka
```

depending on infrastructure and throughput requirements.

This would provide stronger delivery guarantees and allow independent worker scaling.

### Concurrency Handling

Introduce optimistic concurrency using a row-version column or another concurrency strategy.

This would protect against two requests or events attempting to update the same work order at the same time.

### Event Processing States

Instead of only storing successfully processed event IDs, event records could contain states such as:

```text
Received
Processing
Completed
Failed
```

This would improve observability and recovery.

### Retry and Dead-Letter Handling

Transient failures could be retried using a bounded retry policy.

Events that repeatedly fail could be moved to a dead-letter queue for investigation.

### Validation

More comprehensive request validation could be introduced through a dedicated validation library or endpoint filters.

### Authentication and Authorization

Write endpoints could be protected using an API key, OAuth, or another suitable authentication mechanism.

### Observability

Production monitoring would include:

* structured logging
* metrics
* distributed tracing
* health checks
* queue depth monitoring
* event processing duration
* failure rates

### Containerized Development Environment

Docker Compose could be introduced to start both the API and SQL Server consistently for developers and CI.

## Summary

The solution intentionally balances simplicity with production-minded behaviour.

The major design decisions are:

* ASP.NET Core Minimal APIs for the HTTP layer
* EF Core and SQL Server for persistence
* relational status history
* API DTOs rather than exposing EF entity graphs directly
* bounded `Channel<T>` for asynchronous in-memory event processing
* hosted background service for progress-event processing
* persistent event de-duplication using a unique event identifier
* xUnit unit tests
* HTTP integration testing using `WebApplicationFactory`
* SQLite in-memory for fast isolated integration tests

The implementation stays intentionally small while leaving clear paths for adding durability, scalability, concurrency control, and observability in a production deployment.
