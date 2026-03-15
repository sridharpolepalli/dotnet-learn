# Observability: Logging, Monitoring, Tracing & Telemetry

A **topic-wise guide** to observability in .NET and cloud applications: **logging**, **monitoring**, **tracing**, **correlation IDs**, **distributed logging**, **telemetry**, **Application Insights**, **Log Analytics**, **Splunk**, **ELK**, **AWS CloudWatch**, and **Serilog** integration with **ASP.NET Core** and **ILogger**.

---

## Table of Contents

| # | Topic |
|---|--------|
| 1 | [What is Observability?](#1-what-is-observability) |
| 2 | [Logging](#2-logging) |
| 3 | [ILogger and ASP.NET Core Logging](#3-ilogger-and-aspnet-core-logging) |
| 4 | [Structured vs Unstructured Logging](#4-structured-vs-unstructured-logging) |
| 5 | [Serilog with ASP.NET Core](#5-serilog-with-aspnet-core) |
| 6 | [Correlation ID](#6-correlation-id) |
| 7 | [Distributed Logging](#7-distributed-logging) |
| 8 | [Tracing](#8-tracing) |
| 9 | [Telemetry](#9-telemetry) |
| 10 | [Azure Application Insights](#10-azure-application-insights) |
| 11 | [Azure Log Analytics](#11-azure-log-analytics) |
| 12 | [Splunk](#12-splunk) |
| 13 | [ELK Stack (Elasticsearch, Logstash, Kibana)](#13-elk-stack-elasticsearch-logstash-kibana) |
| 14 | [AWS CloudWatch](#14-aws-cloudwatch) |
| 15 | [Serilog + Application Insights](#15-serilog--application-insights) |
| 16 | [Summary and Comparison](#16-summary-and-comparison) |

---

## 1. What is Observability?

**Observability** is the ability to understand the internal state of a system from its external outputs—primarily **logs**, **metrics**, and **traces**.

| Pillar | Purpose |
|--------|---------|
| **Logging** | Record discrete events and messages (errors, info, debug). |
| **Metrics** | Numeric measurements over time (CPU, latency, request count). |
| **Tracing** | Follow a request across services and components (distributed traces). |

Together they answer: *What happened? How is the system performing? Where did a request go?*

---

## 2. Logging

**Logging** is the practice of writing **log entries** (events, errors, state changes) to one or more **sinks** (console, file, database, cloud).

### Why log?

- **Debugging** — Understand what the app did when something failed.
- **Audit** — Who did what and when.
- **Operational visibility** — Health, performance, and behavior in production.

### Log levels (typical)

| Level | Use case |
|--------|----------|
| **Trace / Verbose** | Very detailed, usually disabled in production. |
| **Debug** | Development and troubleshooting. |
| **Information** | General flow (e.g. "Request started", "User logged in"). |
| **Warning** | Recoverable or unexpected but not fatal (e.g. retry, deprecated API). |
| **Error** | Failures that need attention (e.g. exception in a request). |
| **Critical** | System-level failures (e.g. app cannot start). |

In .NET, these map to `LogLevel` enum: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`.

---

## 3. ILogger and ASP.NET Core Logging

ASP.NET Core uses the **generic `ILogger<T>`** and **`ILoggerFactory`** abstractions. Your code depends on **interfaces**, not a specific implementation—so you can swap providers (Console, Debug, Serilog, Application Insights) without changing application code.

### ILogger<T>

- **T** is usually the class that creates the log (e.g. `ILogger<HomeController>`). It’s used to form a **category** so you can filter logs by namespace/class.
- Injected via **dependency injection** in controllers, services, middleware.

### Basic usage

```csharp
public class RecipeController : ControllerBase
{
    private readonly ILogger<RecipeController> _logger;

    public RecipeController(ILogger<RecipeController> logger)
    {
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRecipe(int id)
    {
        _logger.LogInformation("Fetching recipe {RecipeId}", id);
        try
        {
            var recipe = await _recipeService.GetByIdAsync(id);
            if (recipe == null)
            {
                _logger.LogWarning("Recipe {RecipeId} not found", id);
                return NotFound();
            }
            return Ok(recipe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recipe {RecipeId}", id);
            throw;
        }
    }
}
```

### Important methods

| Method | Use |
|--------|-----|
| `LogTrace`, `LogDebug`, `LogInformation`, `LogWarning`, `LogError`, `LogCritical` | Log at a specific level. |
| `Log(level, message, args)` | Generic log with level. |
| **Message template** | First string parameter: e.g. `"Fetching recipe {RecipeId}"`. Use **named placeholders** so backends can store them as structured properties. |
| **Exception** | Overloads like `LogError(ex, "message")` attach the exception to the log entry. |

### Configuration (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "MyApp.RecipeService": "Debug"
    }
  }
}
```

- **Default** applies to all categories.
- More specific namespaces override (e.g. less noise from ASP.NET Core, more detail from your app).

### Adding providers

By default, `WebApplication.CreateBuilder(args)` adds **Console** and **Debug** providers. You can add more:

```csharp
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();
// Or use a third-party provider (e.g. Serilog) that replaces/wraps the default pipeline
```

---

## 4. Structured vs Unstructured Logging

| Type | Description | Example |
|------|-------------|--------|
| **Unstructured** | Plain text message. Hard to query or aggregate. | `"User 42 ordered product 7 at 10:30"` |
| **Structured** | Message + key-value properties (e.g. JSON). Easy to search and aggregate. | `{ "UserId": 42, "ProductId": 7, "Time": "10:30" }` |

**Best practice:** Use **structured logging** with **named placeholders** in the message template so that backends (Application Insights, Log Analytics, ELK, Splunk) can index and query by fields.

```csharp
// Good: RecipeId is a structured property
_logger.LogInformation("Fetching recipe {RecipeId}", id);

// Avoid: string interpolation loses structure
_logger.LogInformation($"Fetching recipe {id}");
```

Serilog and the Microsoft logging extensions both support structured properties when you use the template form.

---

## 5. Serilog with ASP.NET Core

**Serilog** is a popular **structured logging** library for .NET. It supports **sinks** (where logs go), **enrichers** (add properties to every log), and **filters**.

### Why Serilog?

- Rich **sinks**: file, console, SQL Server, Elasticsearch, Application Insights, etc.
- **Enrichers**: add machine name, environment, thread ID, and—with extra packages—**TraceId** / **SpanId** for correlation.
- **Structured properties** from message templates.
- Easy to plug into ASP.NET Core’s **ILogger** so existing `_logger.LogInformation(...)` code works with Serilog.

### NuGet packages

- **Serilog.AspNetCore** — Integrates Serilog with the ASP.NET Core logging pipeline (use this in web apps).
- **Serilog.Sinks.Console** — Writes to console (often included via AspNetCore package).
- **Serilog.Sinks.File** — Writes to rolling files.
- **Serilog.Enrichers.Environment** — Adds MachineName, etc.
- **Serilog.Enrichers.Thread** — Adds ThreadId.
- **Serilog.Sinks.ApplicationInsights** — Sends logs to Azure Application Insights (see [§15](#15-serilog--application-insights)).

### Basic setup (Program.cs)

```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddSerilog(); // Use Serilog for ILogger

var app = builder.Build();
app.UseSerilogRequestLogging(); // Optional: log each HTTP request

// ... middleware and mapping

try
{
    await app.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}
```

### appsettings.json (Serilog section)

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.txt",
          "rollingInterval": "Day"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithEnvironmentName" ]
  }
}
```

### Using ILogger with Serilog

Once Serilog is added to the host and `AddSerilog()` is called, **all `ILogger<T>`** usage in your app goes through Serilog. No need to change controller or service code—they keep using `_logger.LogInformation(...)` and get Serilog’s sinks and enrichers.

### Serilog static Log class

You can also use the static **`Log`** class for code that doesn’t have DI (e.g. startup, background jobs):

```csharp
Log.Information("Application starting");
Log.Error(ex, "Startup failed");
```

For request-scoped data (e.g. correlation ID), push properties with **`LogContext.PushProperty`** so they appear on all logs created in that scope.

---

## 6. Correlation ID

A **correlation ID** (or **request ID**) is a single identifier (e.g. GUID) assigned to a request and **propagated** across services and logs. It lets you group all logs and traces for one logical request.

### Why use it?

- In **distributed systems**, one user action can trigger many services. A correlation ID ties every log/span to that action.
- In **Azure Application Insights** or **OpenTelemetry**, **TraceId** and **SpanId** serve a similar role and are often used together with (or instead of) a custom correlation ID.

### How to implement

1. **Generate** at the entry point (e.g. API gateway or first service): `var correlationId = Guid.NewGuid().ToString("N");` or use the trace ID from the current `Activity`.
2. **Propagate** via HTTP header (e.g. `X-Correlation-Id`) to downstream services.
3. **Add to log context** so every log in that request includes it:
   - **Serilog:** `using (LogContext.PushProperty("CorrelationId", correlationId)) { ... }`
   - **ASP.NET Core:** Middleware can set `HttpContext.TraceIdentifier` and/or a custom header; enrichers can read it and add to logs.
4. **OpenTelemetry/Application Insights:** Use the standard **TraceId** (and SpanId) from `Activity.Current`; many logging integrations add these automatically.

### ASP.NET Core TraceIdentifier

`HttpContext.TraceIdentifier` is a built-in per-request ID. You can expose it in responses (e.g. header) and add it to your logging enrichers so logs are correlated by request.

---

## 7. Distributed Logging

**Distributed logging** means **collecting logs from many services/hosts** into a **central place** so you can search, correlate, and analyze them.

### Challenges

- **Volume** — Many services × many log lines.
- **Format** — Different apps may log differently; standardize (e.g. JSON, common schema).
- **Correlation** — Need TraceId/CorrelationId (and optionally SpanId) in every log.
- **Retention and cost** — Storage and ingestion costs; often need sampling or filtering.

### Common approach

1. **Structured logs** with TraceId/CorrelationId (and SpanId if using tracing).
2. **Agents or SDKs** that send logs to a central backend (Application Insights, Log Analytics, Splunk, ELK, CloudWatch).
3. **Same correlation ID** (or W3C Trace Context) propagated across services so one query can return all related logs.

---

## 8. Tracing

**Tracing** (distributed tracing) tracks a **request** as it flows through **multiple services** and **components**. Each unit of work is a **span**; spans are linked by **TraceId** and **SpanId** to form a **trace**.

### Concepts

| Term | Meaning |
|------|---------|
| **Trace** | End-to-end journey of one request (e.g. API → Auth → DB → Cache). |
| **Span** | One operation within that journey (e.g. "HTTP GET /api/recipe/1", "SQL SELECT"). |
| **TraceId** | Unique ID for the whole trace (same across all spans). |
| **SpanId** | Unique ID for one span. Parent-child relationships form the trace tree. |

### Propagation

- **W3C Trace Context** (traceparent/tracestate headers) is the standard way to pass TraceId and SpanId between HTTP services.
- **OpenTelemetry** and **Application Insights** support W3C and can auto-instrument HTTP, gRPC, and messaging to create and propagate spans.

### In .NET

- **System.Diagnostics.Activity** — .NET’s built-in type for a span; has `Id`, `TraceId`, `ParentId`, etc.
- **OpenTelemetry .NET** — Add instrumentation (e.g. `AddAspNetCoreInstrumentation()`) to create activities and export traces to Application Insights, Jaeger, etc.
- **Application Insights SDK** — Can also create and export traces; integrates with the same W3C context.

### Logs and traces together

- **Log correlation:** Ensure each log entry includes **TraceId** (and ideally SpanId) from `Activity.Current` so logs can be tied to the same trace in your logging backend.
- Serilog enrichers and Application Insights both can pull `Activity.Current?.TraceId` and add it to every log.

---

## 9. Telemetry

**Telemetry** is **data emitted by the application** about its behavior and environment—logs, metrics, traces, and custom events. Collecting and analyzing telemetry is the basis of **monitoring** and **observability**.

### Types of telemetry

| Type | Examples |
|------|----------|
| **Logs / Traces** | Text or structured log entries, request traces. |
| **Metrics** | Counters (request count), gauges (CPU), histograms (latency). |
| **Events** | Custom business or diagnostic events (e.g. "OrderPlaced", "PaymentFailed"). |
| **Dependencies** | Outgoing HTTP, DB, or queue calls (often captured automatically). |
| **Exceptions** | Exception telemetry with stack and context. |

In **Azure Application Insights**, these map to: `TraceTelemetry`, `MetricTelemetry`, `EventTelemetry`, `DependencyTelemetry`, `ExceptionTelemetry`, `RequestTelemetry`.

---

## 10. Azure Application Insights

**Application Insights** is Azure’s **application performance management (APM)** and **observability** service. It collects **telemetry** (requests, dependencies, exceptions, traces, custom events, metrics) from your app and stores it in **Azure Monitor** (in a **Log Analytics workspace**).

### What it gives you

- **Request performance** — Response times, failure rates, throughput.
- **Dependency tracking** — Calls to DB, HTTP, queues; failures and latency.
- **Exceptions** — With stack traces and context.
- **Custom events and metrics** — Business and custom KPIs.
- **Live metrics** — Near real-time stream (optional).
- **Distributed tracing** — TraceId/SpanId across services when using the same workspace and SDK.

### Adding to ASP.NET Core

```csharp
builder.Services.AddApplicationInsightsTelemetry();
// Or with connection string:
// builder.Services.AddApplicationInsightsTelemetry("InstrumentationKey-or-ConnectionString");
```

Connection is usually read from **Application Insights** connection string or instrumentation key in config (e.g. `APPLICATIONINSIGHTS_CONNECTION_STRING` in App Service).

### Data model (high level)

- **Requests** — Incoming HTTP requests.
- **Dependencies** — Outgoing calls.
- **Traces** — Log-like messages.
- **Exceptions** — Exception records.
- **Events** — Custom events.
- **Metrics** — Numeric metrics.

This data is stored in the associated **Log Analytics** workspace and queryable via **KQL** (Kusto Query Language).

---

## 11. Azure Log Analytics

**Log Analytics** is the **log storage and query** engine inside **Azure Monitor**. It uses **workspaces** and **Kusto (KQL)** for querying.

### Relationship to Application Insights

- **Application Insights** stores its telemetry in a **Log Analytics workspace**.
- **Log Analytics** also ingests logs from many other sources: VMs (agents), diagnostic settings (e.g. storage, Service Bus), custom log uploads, etc.

So: **Log Analytics** = storage + query; **Application Insights** = app-focused ingestion and UX on top of that data.

### Common tables (when using Application Insights)

- `requests` — Incoming requests.
- `dependencies` — Outgoing dependencies.
- `traces` — Log/trace messages (e.g. from ILogger or Serilog).
- `exceptions` — Exceptions.
- `customEvents` — Custom events.

You run **KQL** in the Log Analytics or Application Insights query editor to filter, aggregate, and join these tables.

### Example KQL (traces for a TraceId)

```kusto
traces
| where operation_Id == "your-trace-id"
| order by timestamp asc
```

---

## 12. Splunk

**Splunk** is a commercial **log aggregation and analysis** platform. It ingests logs (files, HTTP, syslog, etc.), indexes them, and provides search, dashboards, and alerting.

### Use with .NET

- **Splunk HTTP Event Collector (HEC)** — Send JSON logs over HTTP; many logging libraries have a Splunk sink or you can use a generic HTTP sink.
- **Serilog:** Community sinks (e.g. **Serilog.Sinks.Splunk**) send structured logs to Splunk.
- **Agents** — Splunk Universal Forwarder on the machine can read log files and forward them.

You still use **ILogger** or **Serilog** in the app; the sink or forwarder handles sending to Splunk. Correlation ID / TraceId should be included as structured fields so you can search by them in Splunk.

---

## 13. ELK Stack (Elasticsearch, Logstash, Kibana)

**ELK** is a popular **open-source** stack for log aggregation and search:

| Component | Role |
|-----------|------|
| **Elasticsearch** | Search engine and store for log documents. |
| **Logstash** | Ingests, transforms, and forwards logs (optional; sometimes replaced by Filebeat or direct SDK). |
| **Kibana** | UI for search, dashboards, and visualization. |

**Filebeat** is often used instead of (or with) Logstash as a lightweight shipper that reads log files and sends to Elasticsearch.

### Use with .NET

- **Serilog.Sinks.Elasticsearch** — Writes Serilog events directly to Elasticsearch (JSON documents).
- **NLog / ILogger** — Various extensions and sinks can target Elasticsearch or Logstash.
- **OpenTelemetry** — Can export traces/metrics to Elastic APM or Elasticsearch.

Best practice: **structured logs** (e.g. JSON) with **TraceId** and **SpanId** so you can correlate with trace data in Elastic APM or custom dashboards.

---

## 14. AWS CloudWatch

**Amazon CloudWatch** is AWS’s **monitoring and observability** service: **logs**, **metrics**, and (with X-Ray) **tracing**.

### Logs

- **Log groups** and **log streams** — Applications (or agents) send log events to CloudWatch Logs.
- **CloudWatch Logs Insights** — Query language over log data (similar in spirit to KQL or Splunk).

### Metrics

- **Built-in metrics** — EC2, RDS, Lambda, etc.
- **Custom metrics** — Emit from your app via SDK or agent.

### Tracing

- **AWS X-Ray** — Distributed tracing; integrates with Lambda, ECS, and many AWS services. Can correlate with logs if you embed TraceId in log messages.

### Use with .NET

- **Serilog.Sinks.AwsCloudWatch** — Serilog sink that writes to CloudWatch Logs.
- **AWS SDK** — Emit custom metrics and log events from .NET.
- **OpenTelemetry** — Export traces to X-Ray using the OpenTelemetry X-Ray exporter.

Correlation ID or X-Ray TraceId in every log line helps when debugging across services.

---

## 15. Serilog + Application Insights

You can use **Serilog** for structured logging and **send those logs to Application Insights** so they appear as **traces** (or events) and are queryable in Log Analytics with the same **TraceId** as automatic request/dependency telemetry.

### NuGet

- **Serilog.Sinks.ApplicationInsights** — Sink that converts Serilog `LogEvent` to Application Insights telemetry.

### Recommended: use existing TelemetryConfiguration

Prefer passing the **same `TelemetryConfiguration`** that the app uses (from DI), so one configuration and same sampling/context:

```csharp
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Resolve TelemetryConfiguration from DI (after Build())
var telemetryConfiguration = app.Services.GetRequiredService<Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration>();

Log.Logger = new LoggerConfiguration()
    .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces)
    .CreateLogger();

builder.Host.UseSerilog(); // If not already configured above
```

### TelemetryConverter

- **TelemetryConverter.Traces** — Serilog events become **TraceTelemetry**; show up in Application Insights "Traces" and in Log Analytics `traces` table. Good for correlation with requests.
- **TelemetryConverter.Events** — Serilog events become **EventTelemetry**; show up as custom events.

Log entries that include an **exception** are sent as **ExceptionTelemetry** regardless of converter.

### Enrichers for TraceId / correlation

To correlate Serilog logs with Application Insights requests, add **TraceId** and **SpanId** from the current `Activity` to each log. You can use a custom enricher or a package that adds `Activity` properties:

```csharp
.Enrich.WithProperty("TraceId", () => System.Diagnostics.Activity.Current?.TraceId.ToString())
.Enrich.WithProperty("SpanId", () => System.Diagnostics.Activity.Current?.SpanId.ToString())
```

Then when logs are sent to Application Insights, they share the same operation context as the request telemetry.

### ILogger and Serilog together

- Use **Serilog** as the logging provider (`UseSerilog`, `AddSerilog`).
- All **ILogger&lt;T&gt;** calls go through Serilog and can be sent to Application Insights via the sink above.
- No need to use the static `Log` class everywhere; **ILogger** is the recommended abstraction.

---

## 16. Summary and Comparison

| Topic | Brief summary |
|--------|----------------|
| **Observability** | Logs + metrics + traces to understand system behavior. |
| **Logging** | Recording discrete events with levels and structure. |
| **ILogger** | .NET’s standard logging abstraction; use in controllers and services. |
| **Structured logging** | Message template + properties; enables querying and correlation. |
| **Serilog** | Structured logging library with sinks and enrichers; integrates with ILogger. |
| **Correlation ID** | Single ID per request propagated across services and logs. |
| **Distributed logging** | Central collection of logs from many services with correlation. |
| **Tracing** | TraceId/SpanId and spans across services (e.g. OpenTelemetry, Application Insights). |
| **Telemetry** | All emitted data: logs, metrics, traces, events, exceptions. |
| **Application Insights** | Azure APM: requests, dependencies, traces, exceptions, custom events; data in Log Analytics. |
| **Log Analytics** | Azure log store and KQL; backs Application Insights and other sources. |
| **Splunk** | Commercial log aggregation; use HEC or Serilog sink. |
| **ELK** | Elasticsearch + Logstash/Filebeat + Kibana for log storage and search. |
| **CloudWatch** | AWS logs, metrics, and (with X-Ray) tracing. |
| **Serilog + App Insights** | Serilog.Sinks.ApplicationInsights with TelemetryConverter.Traces; use DI TelemetryConfiguration and enrichers for TraceId. |

### Suggested learning order

1. **ILogger** and ASP.NET Core logging (levels, configuration, message templates).  
2. **Structured logging** and why it matters.  
3. **Serilog** (sinks, enrichers, integration with ILogger).  
4. **Correlation ID** and **distributed logging**.  
5. **Tracing** (spans, TraceId, W3C, OpenTelemetry or Application Insights).  
6. **Telemetry** and **Application Insights** (requests, dependencies, traces, KQL).  
7. **Log Analytics** (workspace, tables, KQL).  
8. **Serilog + Application Insights** (sink, enrichers, TraceId).  
9. **Splunk, ELK, CloudWatch** as alternative backends and when to use them.

---

*This guide aligns with common practices from the [Serilog with Azure Application Insights](https://chatgpt.com/share/69b376c9-d184-800e-b5b4-ef2e18c89197) discussion and Microsoft/OpenTelemetry documentation.*
