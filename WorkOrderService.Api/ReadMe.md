# Work Order Service

A small production-minded backend service built with **.NET 8**, **ASP.NET Core Minimal APIs**, **Entity Framework Core**, and **SQL Server**.

The service manages work orders, keeps status change history, and processes external progress events asynchronously through an in-memory queue and hosted background worker.

## Tech Stack

* .NET 8
* ASP.NET Core Minimal APIs
* Entity Framework Core 8
* SQL Server / LocalDB
* SQLite in-memory for integration tests
* xUnit
* Swagger / OpenAPI

## Project Structure

```text
WorkOrderService.Api/
├── BackgroundProcessing/
│   ├── IProgressEventQueue.cs
│   ├── ProgressEventQueue.cs
│   └── ProgressEventWorker.cs
├── Contracts/
│   ├── CreateWorkOrderRequest.cs
│   ├── UpdateWorkOrderStatusRequest.cs
│   ├── ProgressEventRequest.cs
│   └── WorkOrderResponse.cs
├── Domain/
│   ├── WorkOrder.cs
│   ├── WorkOrderStatus.cs
│   ├── WorkOrderStatusHistory.cs
│   └── ProcessedProgressEvent.cs
├── Endpoints/
│   ├── WorkOrderEndpoints.cs
│   └── ProgressEventEndpoints.cs
├── Migrations/
├── Persistence/
│   └── AppDbContext.cs
├── Program.cs
└── appsettings.json

WorkOrderService.Tests/
├── Integration.cs
└── Unit.cs
```

## Prerequisites

Before running the project, ensure the following are installed:

* .NET 8 SDK
* SQL Server LocalDB or another SQL Server instance

You can check your installed .NET SDKs with:

```bash
dotnet --list-sdks
```

## Database Configuration

The application uses SQL Server through Entity Framework Core.

Example connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=WorkOrderServiceDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Update the connection string if you are using another SQL Server instance.

## Running the Application

Restore dependencies:

```bash
dotnet restore
```

Apply the Entity Framework migrations:

```bash
dotnet ef database update
```

Run the API:

```bash
dotnet run --project WorkOrderService.Api
```

Swagger will be available at a URL similar to:

```text
https://localhost:xxxx/swagger
```

The exact port is displayed when the application starts.

## API Endpoints

### Create Work Order

```http
POST /api/work-orders
```

Example request:

```json
{
  "externalId": "WO-1001",
  "siteCode": "JHB-001",
  "description": "Install network equipment"
}
```

New work orders are created with an initial status of `Pending`.

Example response:

```json
{
  "id": 1,
  "externalId": "WO-1001",
  "siteCode": "JHB-001",
  "description": "Install network equipment",
  "status": "Pending",
  "createdAt": "2026-08-23T15:00:00Z",
  "updatedAt": "2026-08-23T15:00:00Z",
  "statusHistory": []
}
```

Response status:

```text
201 Created
```

### Get Work Order

```http
GET /api/work-orders/{id}
```

Example:

```http
GET /api/work-orders/1
```

The response includes the work order's status change history.

Example:

```json
{
  "id": 1,
  "externalId": "WO-1001",
  "siteCode": "JHB-001",
  "description": "Install network equipment",
  "status": "InProgress",
  "createdAt": "2026-08-23T15:00:00Z",
  "updatedAt": "2026-08-23T16:00:00Z",
  "statusHistory": [
    {
      "fromStatus": "Pending",
      "toStatus": "InProgress",
      "changedAt": "2026-08-23T16:00:00Z"
    }
  ]
}
```

If the work order does not exist:

```text
404 Not Found
```

### Update Work Order Status

```http
PATCH /api/work-orders/{id}/status
```

Example request:

```json
{
  "status": "InProgress"
}
```

Updating the status also creates a new status history record.

### List Work Orders

```http
GET /api/work-orders
```

The endpoint uses a fixed page size.

Work orders can optionally be filtered by status:

```http
GET /api/work-orders?status=Pending
```

Example statuses:

```text
Pending
InProgress
Completed
Cancelled
```

## Progress Events

External systems can submit progress events using:

```http
POST /api/progress-events
```

Example request:

```json
{
  "eventId": "evt-001",
  "workOrderExternalId": "WO-1001",
  "newStatus": "Completed",
  "occurredAt": "2026-08-23T17:00:00Z",
  "details": "Installation completed successfully"
}
```

The endpoint places the event onto an in-memory queue and returns:

```text
202 Accepted
```

The event is processed asynchronously by a hosted background service.

The background worker:

1. Reads the event from the queue.
2. Checks whether the event ID has already been processed.
3. Finds the referenced work order.
4. Updates its status.
5. Adds a status history record.
6. Stores the processed event ID.

Submitting the same `eventId` more than once does not create multiple effects.

## Database Model

The main tables are:

```text
WorkOrders
WorkOrderStatusHistories
ProcessedProgressEvents
```

`WorkOrderStatusHistories` has a foreign key to `WorkOrders`.

`ProcessedProgressEvents.EventId` has a unique index to support event de-duplication.

`WorkOrders.ExternalId` is also unique.

## Entity Framework Migrations

Create a new migration:

```bash
dotnet ef migrations add MigrationName --project WorkOrderService.Api
```

Apply migrations:

```bash
dotnet ef database update --project WorkOrderService.Api
```

## Running Tests

Run all tests from the solution directory:

```bash
dotnet test
```

The test suite contains:

* unit tests for business behaviour
* an integration test that calls the Work Orders API through an HTTP test server

The application uses SQL Server in normal operation.

Integration tests use **SQLite in-memory** to keep tests isolated and fast.

The integration test uses `WebApplicationFactory<Program>` to start the ASP.NET Core application in a test environment and replaces the SQL Server EF Core provider with SQLite.

## Swagger

Swagger is enabled in the development environment and can be used to test all HTTP endpoints interactively.

Run the application and navigate to:

```text
/swagger
```

on the HTTPS URL shown in the console.

## Notes

The in-memory queue is intentionally lightweight to match the scope of the assignment.

Because it is not durable, queued events can be lost if the application terminates before they are processed. A production deployment requiring guaranteed event delivery would normally use a durable messaging platform such as Azure Service Bus, RabbitMQ, or another suitable messaging system.
