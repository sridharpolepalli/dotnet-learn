# Observability Demo — Step-by-Step Implementation

This document walks through a **real-time use case** with **2 microservices**, **1 Hangfire background job**, **correlation ID** propagation, and **Application Insights** as the telemetry store. It maps each step to the observability concepts: logging, levels, structured logging, tracing, telemetry, correlation ID, distributed logging, sink, and enrichers.

---

## What You Will See

| Concept | Where it appears in the demo |
|--------|-------------------------------|
| **Observability** | Understanding the full flow (OrdersApi → InventoryApi → Hangfire job) from logs, metrics, and traces in Application Insights. |
| **Logging** | Discrete events with levels (Information, Debug, Warning, Error) in both APIs and the job. |
| **Log levels** | `LogDebug`, `LogInformation`, `LogWarning`, `LogError` used in code; config in `appsettings.json`. |
| **Structured logging** | Message templates like `"CreateOrder started. {ProductId}, {Quantity}, {CustomerId}"` with named properties. |
| **Monitoring** | Application Insights collects, stores, and visualizes telemetry; you can set alerts. |
| **Tracing** | TraceId/SpanId on each request; W3C `traceparent` forwarded to InventoryApi; dependency from OrdersApi → InventoryApi. |
| **Telemetry** | Requests, dependencies, traces, exceptions (and custom events) sent to Application Insights. |
| **Correlation ID** | `X-Correlation-Id` generated in OrdersApi, forwarded to InventoryApi and into Hangfire job; same ID on all related logs. |
| **Distributed logging** | Logs from OrdersApi, InventoryApi, and the background job stored in one place (Application Insights) and queryable by CorrelationId or TraceId. |
| **Sink** | Application Insights (and Console) as destinations for Serilog. **Enricher** = CorrelationId, TraceId, SpanId, MachineName, Application. **Message template** = structured message with placeholders. |

---

## Architecture

```
Client
  │
  ▼ POST /api/orders
┌─────────────────────────────────────────────────────────┐
│ OrdersApi (port 5000)                                   │
│  - CorrelationIdMiddleware (set/forward X-Correlation-Id)│
│  - Serilog + App Insights sink, enrichers (CorrelationId,│
│    TraceId, SpanId)                                      │
│  - POST /api/orders → calls InventoryApi → enqueue job  │
└──────────────────────────┬──────────────────────────────┘
                           │ GET /api/inventory/check
                           │ Header: X-Correlation-Id
                           ▼
┌─────────────────────────────────────────────────────────┐
│ InventoryApi (port 5001)                                │
│  - CorrelationIdMiddleware (read X-Correlation-Id)       │
│  - Serilog + App Insights                               │
│  - GET /api/inventory/check?productId=PROD-001          │
└─────────────────────────────────────────────────────────┘

OrdersApi also enqueues:
┌─────────────────────────────────────────────────────────┐
│ Hangfire job (OrderFulfillmentJob)                      │
│  - Receives correlationId as argument                  │
│  - LogContext.PushProperty("CorrelationId", ...)        │
│  - Logs appear in App Insights with same CorrelationId  │
└─────────────────────────────────────────────────────────┘

                    ▼ All telemetry
         ┌──────────────────────────────┐
         │ Azure Application Insights   │
         │ (requests, dependencies,     │
         │  traces, custom properties)  │
         └──────────────────────────────┘
```

---

## Prerequisites

- .NET 8 SDK
- Azure subscription (for Application Insights) or run without connection string to see console + Hangfire only

---

## Step 1: Clone / open the solution

From the repo root:

```bash
cd ObservabilityDemo
dotnet restore
```

Solution layout:

- **ObservabilityDemo.Common** — Correlation ID constants, helper, shared DTOs
- **ObservabilityDemo.OrdersApi** — API + Hangfire job; calls InventoryApi
- **ObservabilityDemo.InventoryApi** — Inventory check API

---

## Step 2: Create an Application Insights resource (optional but recommended)

1. In Azure Portal: **Create a resource** → **Application Insights**.
2. Create a new resource group or use existing; set **Region** and **Workspace** (Log Analytics).
3. After creation, open the resource → **Overview** → copy **Connection string**.

---

## Step 3: Configure connection strings

**OrdersApi**

Edit `src/ObservabilityDemo.OrdersApi/appsettings.Development.json` (or set user secrets / env):

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=https://...;..."
  }
}
```

**InventoryApi**

Edit `src/ObservabilityDemo.InventoryApi/appsettings.Development.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=https://...;..."
  }
}
```

Use the **same** Application Insights resource for both APIs so all telemetry is in one place (distributed logging).

If you leave the connection string empty, the app still runs; logs go only to the console.

---

## Step 4: Run InventoryApi first

Terminal 1:

```bash
cd src/ObservabilityDemo.InventoryApi
dotnet run
```

It listens on **http://localhost:5001**. You should see Serilog output in the console (structured lines with properties).

---

## Step 5: Run OrdersApi

Terminal 2:

```bash
cd src/ObservabilityDemo.OrdersApi
dotnet run
```

It listens on **http://localhost:5000**. Ensure `appsettings.json` has `InventoryApi:BaseUrl": "http://localhost:5001"` (default).

---

## Step 6: Send a request that ties everything together

From PowerShell (or curl / Postman):

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/api/orders" `
  -ContentType "application/json" `
  -Body '{"ProductId":"PROD-001","Quantity":2,"CustomerId":"CUST-1"}'
```

Or with a custom correlation ID to trace it end-to-end:

```powershell
$headers = @{ "Content-Type" = "application/json"; "X-Correlation-Id" = "my-test-id-12345" }
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/api/orders" -Headers $headers `
  -Body '{"ProductId":"PROD-001","Quantity":2,"CustomerId":"CUST-1"}'
```

Expected response (shape):

```json
{
  "orderId": "ORD-...",
  "productId": "PROD-001",
  "quantity": 2,
  "status": "Accepted",
  "createdAt": "2025-03-15T..."
}
```

---

## Step 7: What happens (flow and concepts)

1. **Request hits OrdersApi**  
   - **CorrelationIdMiddleware**: No `X-Correlation-Id` → generates one (e.g. `a1b2c3d4...`); sets `HttpContext.Items` and response header; pushes to **LogContext** (enricher).  
   - **Serilog request logging**: Logs HTTP request with **message template** and **structured properties**.  
   - **Observability**: One trace (TraceId) and one correlation ID for the whole flow.

2. **OrdersController.CreateOrder**  
   - **Logging**: `LogInformation("CreateOrder started. {ProductId}, {Quantity}, {CustomerId}", ...)` — **structured logging**.  
   - **Log levels**: `LogDebug` for correlation ID, `LogInformation` for flow, `LogWarning` if insufficient stock, `LogError` if inventory call fails.  
   - **Telemetry**: Request telemetry and custom trace messages (from Serilog) go to Application Insights.

3. **HttpClient call to InventoryApi**  
   - **CorrelationIdDelegatingHandler** adds **X-Correlation-Id** and **traceparent** (W3C) to the outgoing request.  
   - **Tracing**: Creates a **dependency** span (OrdersApi → InventoryApi); **distributed tracing**.  
   - **Distributed logging**: Same correlation ID (and trace) on both sides.

4. **InventoryApi**  
   - **CorrelationIdMiddleware** reads **X-Correlation-Id**; pushes to **LogContext**.  
   - **Structured logging**: `"Inventory check result. {ProductId}, {AvailableQuantity}, {InStock}"`.  
   - All InventoryApi logs for this request have the **same CorrelationId** and can be part of the same **trace**.

5. **Back in OrdersApi**  
   - **Hangfire**: `BackgroundJob.Enqueue<OrderFulfillmentJob>(j => j.ProcessOrderAsync(..., correlationId))`.  
   - **Correlation ID** is passed into the job so the background work is tied to the same logical request.

6. **OrderFulfillmentJob**  
   - **LogContext.PushProperty("CorrelationId", correlationId)** at the start of the job.  
   - **Enricher**: Every log inside the job gets **CorrelationId** without changing every log call.  
   - **Sink**: Those logs go to Application Insights (and console) like the API logs.  
   - **Distributed logging**: In Application Insights you can filter by this CorrelationId and see API + job logs together.

---

## Step 8: Sample output

### Console (OrdersApi)

Example lines (format may vary by Serilog config):

```
[INF] CreateOrder started. PROD-001, 2, CUST-1
[DBG] CorrelationId for this request: a1b2c3d4e5f6...
[INF] Order ORD-abc123 created for product PROD-001, quantity 2
[INF] CreateOrder completed. ORD-abc123, Accepted
[INF] Background job started. ORD-abc123, PROD-001, 2
[INF] Background job completed. ORD-abc123
```

Each line is produced by **structured logging** (message template + properties). With the App Insights sink, the **same events** appear in Azure as **traces** with **CorrelationId**, **TraceId**, **SpanId**, and other enricher properties.

### Console (InventoryApi)

```
[DBG] Check inventory for PROD-001
[INF] Inventory check result. PROD-001, 100, True
```

### Application Insights (what you see in the portal)

- **Transaction search / Logs**  
  - **traces** table: Serilog messages with custom dimensions (e.g. `CorrelationId`, `ProductId`, `OrderId`, `TraceId`, `SpanId`).  
  - **requests** table: Incoming HTTP requests (e.g. POST /api/orders, GET /api/inventory/check).  
  - **dependencies** table: Outgoing call from OrdersApi to InventoryApi (with duration, success/failure).  
  - **exceptions** table: Any `LogError(ex, ...)` or unhandled exceptions.

- **KQL examples (Log Analytics / Application Insights → Logs)**

  All logs for one correlation ID (distributed logging):

  ```kusto
  union traces, requests, dependencies
  | where customDimensions.CorrelationId == "my-test-id-12345"
  | order by timestamp asc
  ```

  Or by operation Id (one trace):

  ```kusto
  union traces, requests, dependencies
  | where operation_Id == "<trace-id-from-request>"
  | order by timestamp asc
  ```

  Traces that contain a given order ID (structured logging):

  ```kusto
  traces
  | where customDimensions.OrderId == "ORD-abc123"
  | project timestamp, message, customDimensions
  ```

- **Application map**  
  Shows OrdersApi and InventoryApi with dependency between them (tracing).

- **Request → Trace correlation**  
  Open a request, then “View all telemetry for this request” to see the trace tree and related logs (TraceId/SpanId and correlation).

---

## Step 9: Hangfire dashboard

Open **http://localhost:5000/hangfire** to see:

- **Enqueued** and **Processing** / **Succeeded** jobs.  
- Each job run is triggered by a real order request; the job logs use the **same CorrelationId** as that request, so in Application Insights you see the full story: API logs + dependency + background job logs.

---

## Summary: concept → implementation

| Term | Implementation in this demo |
|------|-----------------------------|
| **Observability** | Logs + metrics + traces in Application Insights; one place to understand OrdersApi, InventoryApi, and Hangfire. |
| **Logging** | ILogger + Serilog in both APIs and OrderFulfillmentJob; levels from config. |
| **Log levels** | Trace/Debug/Information/Warning/Error in code; overrides in appsettings. |
| **Structured logging** | Message templates with `{ProductId}`, `{OrderId}`, etc.; properties in customDimensions in App Insights. |
| **Monitoring** | Application Insights stores and visualizes; you can add alerts on metrics or log-based metrics. |
| **Tracing** | TraceId/SpanId (Activity); W3C traceparent to InventoryApi; dependency and request telemetry. |
| **Telemetry** | Requests, dependencies, traces, exceptions (and custom dimensions) sent to Application Insights. |
| **Correlation ID** | Middleware + LogContext in both APIs; passed to Hangfire job and pushed to LogContext in job. |
| **Distributed logging** | Same App Insights resource; query by CorrelationId or operation_Id across APIs and job. |
| **Sink** | Application Insights (and Console). **Enricher**: CorrelationId, TraceId, SpanId, MachineName, Application. **Message template**: e.g. `"CreateOrder started. {ProductId}, {Quantity}, {CustomerId}"`. |

This gives you a single, runnable use case that produces a **sample output** (console + Application Insights) for every concept in the table.
