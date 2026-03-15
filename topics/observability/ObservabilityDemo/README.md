# Observability Demo

A minimal **real-time use case** to practice observability with **2 microservices**, **Hangfire**, **correlation ID**, and **Application Insights**.

## What's in this repo

| Project | Description |
|--------|-------------|
| **ObservabilityDemo.Common** | Correlation ID constants, helper, shared DTOs (OrderRequest, OrderResponse, InventoryResponse). |
| **ObservabilityDemo.OrdersApi** | ASP.NET Core API on port 5000. POST `/api/orders` → calls InventoryApi → enqueues Hangfire job. Uses Serilog, Application Insights, correlation middleware, and forwards `X-Correlation-Id` to InventoryApi. |
| **ObservabilityDemo.InventoryApi** | ASP.NET Core API on port 5001. GET `/api/inventory/check?productId=...` returns stock. Uses Serilog, Application Insights, correlation middleware. |

## Quick start

1. **Restore and build**  
   This folder includes a `nuget.config` that uses only nuget.org (so the demo works without private feeds).
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Set Application Insights** (optional)  
   In both APIs, set `ApplicationInsights:ConnectionString` in `appsettings.Development.json` or via environment variable. If not set, logs only go to the console.

3. **Run both APIs**
   - Terminal 1: `cd src/ObservabilityDemo.InventoryApi && dotnet run`
   - Terminal 2: `cd src/ObservabilityDemo.OrdersApi && dotnet run`

4. **Create an order**
   ```bash
   curl -X POST http://localhost:5000/api/orders -H "Content-Type: application/json" -d "{\"ProductId\":\"PROD-001\",\"Quantity\":2,\"CustomerId\":\"CUST-1\"}"
   ```

5. **Inspect**
   - Console: structured logs with CorrelationId, TraceId, SpanId.
   - Application Insights: requests, dependencies, traces; query by `customDimensions.CorrelationId` or `operation_Id`.
   - Hangfire: http://localhost:5000/hangfire

## Step-by-step and sample output

See **[docs/Observability-Demo-Implementation.md](./docs/Observability-Demo-Implementation.md)** for:

- How each observability term (logging, levels, structured logging, tracing, telemetry, correlation ID, distributed logging, sink, enricher, message template) is implemented.
- End-to-end flow and architecture.
- Example KQL queries and what you see in Application Insights.
