# Observability: Logging, Monitoring, Tracing & Telemetry

A learner-focused guide to observability: core **terminology**, **technologies** by category, and **usage in .NET Core**.

---

## Table of Contents

### Part I — Terminology

| # | Topic |
|---|--------|
| 1 | [Observability](#1-observability) |
| 2 | [Logging and log levels](#2-logging-and-log-levels) |
| 3 | [Structured vs unstructured logging](#3-structured-vs-unstructured-logging) |
| 4 | [Monitoring](#4-monitoring) |
| 5 | [Tracing](#5-tracing) |
| 6 | [Telemetry](#6-telemetry) |
| 7 | [Correlation ID](#7-correlation-id) |
| 8 | [Distributed logging](#8-distributed-logging) |
| 9 | [Sink, enricher, and message template](#9-sink-enricher-and-message-template) |

### Part II — Technologies (by category)

| # | Topic |
|---|--------|
| 10 | [Azure: Application Insights](#10-azure-application-insights) |
| 11 | [Azure: Log Analytics](#11-azure-log-analytics) |
| 12 | [Splunk](#12-splunk) |
| 13 | [ELK Stack (Elasticsearch, Logstash, Kibana)](#13-elk-stack-elasticsearch-logstash-kibana) |
| 14 | [AWS CloudWatch](#14-aws-cloudwatch) |

### Part III — Usage in .NET Core

| # | Topic |
|---|--------|
| 15 | [ILogger and ASP.NET Core logging](#15-ilogger-and-aspnet-core-logging) |
| 16 | [Serilog with ASP.NET Core](#16-serilog-with-aspnet-core) |
| 17 | [Serilog + Application Insights in .NET Core](#17-serilog--application-insights-in-net-core) |

### Reference

| # | Topic |
|---|--------|
| 18 | [Summary and learning sequence](#18-summary-and-learning-sequence) |

---

# Part I — Terminology

## 1. Observability

**Observability** is the ability to infer the **internal state** of a system from its **external outputs**. You do not instrument every internal variable; instead you rely on signals the system emits (logs, metrics, traces) to understand what is happening.

The three pillars of observability are:

| Pillar | What it is | Question it answers |
|--------|------------|----------------------|
| **Logging** | Discrete events and messages (errors, info, debug). | *What happened?* |
| **Metrics** | Numeric measurements over time (CPU, latency, request count). | *How is the system performing?* |
| **Tracing** | Following a request across services and components. | *Where did the request go, and how long did each step take?* |

Together, these let you debug failures, tune performance, and understand behavior in production.

---

## 2. Logging and log levels

**Logging** is the practice of writing **log entries**—records of events, errors, or state changes—to one or more destinations (console, file, database, or a cloud service). Each entry typically has a **level**, a **message**, and optional **properties**.

### Why we log

- **Debugging** — Understand what the application did when something failed.
- **Audit** — Record who did what and when (e.g. compliance, security).
- **Operational visibility** — See health, performance, and behavior in production without attaching a debugger.

### Log levels

A **log level** indicates severity or detail. You configure which level is written (e.g. only Warning and above in production) so that you can reduce noise and cost while keeping important information.

| Level | Typical use |
|--------|-------------|
| **Trace / Verbose** | Very detailed diagnostic data; usually disabled in production. |
| **Debug** | Development and troubleshooting; often disabled in production. |
| **Information** | Normal flow (e.g. "Request started", "User logged in"). |
| **Warning** | Recoverable or unexpected situations (e.g. retry, deprecated API use). |
| **Error** | Failures that need attention (e.g. exception while handling a request). |
| **Critical** | Severe, system-level failures (e.g. app cannot start, out of memory). |

In .NET, these correspond to the `LogLevel` enum: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`.

---

## 3. Structured vs unstructured logging

| Type | Description | Example |
|------|-------------|--------|
| **Unstructured** | Free-form text. Hard to query or aggregate by field. | `"User 42 ordered product 7 at 10:30"` |
| **Structured** | Message plus key-value properties (often JSON). Easy to search and aggregate. | `{ "UserId": 42, "ProductId": 7, "Time": "10:30" }` |

**Structured logging** is the recommended approach: the message is a template (e.g. `"User {UserId} ordered product {ProductId}"`) and each placeholder becomes a **named property**. Backends (Application Insights, Log Analytics, ELK, Splunk) can index and query by these properties instead of parsing raw text.

---

## 4. Monitoring

**Monitoring** is the practice of **continuously observing** a system using logs, metrics, and traces. It includes:

- **Collecting** telemetry (logs, metrics, traces).
- **Storing** and indexing it for querying.
- **Visualizing** it (dashboards, charts).
- **Alerting** when thresholds are breached (e.g. error rate > 5%, latency > 2s).

Monitoring is what you do with the data; observability is the property of the system that makes that data sufficient to understand its behavior.

---

## 5. Tracing

**Tracing** (distributed tracing) tracks a **single logical request** as it moves through **multiple services** and **components**. Each unit of work is a **span**; spans are linked by identifiers to form a **trace**.

### Key terms

| Term | Meaning |
|------|---------|
| **Trace** | The end-to-end journey of one request (e.g. API → Auth service → Database → Cache). |
| **Span** | One operation within that journey (e.g. "HTTP GET /api/orders", "SQL SELECT"). |
| **TraceId** | Unique ID for the entire trace; the same across all spans in that request. |
| **SpanId** | Unique ID for a single span. Parent-child relationships between spans form the trace tree. |

### Propagation

To trace across process boundaries, **TraceId** and **SpanId** must be passed between services. **W3C Trace Context** (HTTP headers `traceparent` and `tracestate`) is the standard way to do this. When a service receives a request with these headers, it creates child spans under the same TraceId so the full path of the request can be reconstructed in one place.

---

## 6. Telemetry

**Telemetry** is **data emitted by the application** about its behavior and environment. It is the raw material of observability and monitoring.

### Types of telemetry

| Type | Examples |
|------|----------|
| **Logs / traces** | Text or structured log entries; request trace messages. |
| **Metrics** | Counters (request count), gauges (CPU usage), histograms (latency distribution). |
| **Events** | Custom business or diagnostic events (e.g. "OrderPlaced", "PaymentFailed"). |
| **Dependencies** | Outgoing calls (HTTP, database, queues)—often captured automatically by SDKs. |
| **Exceptions** | Exception records with stack trace and context. |

In Azure Application Insights, these map to types such as `TraceTelemetry`, `MetricTelemetry`, `EventTelemetry`, `DependencyTelemetry`, and `ExceptionTelemetry`.

---

## 7. Correlation ID

A **correlation ID** (or **request ID**) is a **single identifier** (e.g. a GUID) assigned to one logical request and **propagated** to every service and log entry involved in handling that request.

### Why it matters

In distributed systems, one user action can trigger many services. Without a correlation ID, logs from different services are hard to tie together. With it, you can search for one ID and see the full story across all services.

**TraceId** (from W3C Trace Context or OpenTelemetry) serves a similar role and is often used together with or instead of a custom correlation ID. Many systems use TraceId as the primary correlation key.

### How it is used

1. **Generate** at the entry point (e.g. API gateway or first service).
2. **Propagate** via HTTP header (e.g. `X-Correlation-Id`) to downstream services.
3. **Add to log context** so every log entry in that request includes the ID (e.g. via middleware and logging enrichers).

---

## 8. Distributed logging

**Distributed logging** means **collecting logs from many services and hosts** into a **central store** so you can search, correlate, and analyze them in one place.

### Challenges

- **Volume** — Many services and instances produce huge numbers of log lines.
- **Format** — Different apps may log differently; standardizing (e.g. JSON, common schema) helps.
- **Correlation** — Every log should carry TraceId or CorrelationId so you can group by request.
- **Cost and retention** — Storage and ingestion have cost; sampling and retention policies are important.

### Common approach

1. Use **structured logs** with **TraceId** or **CorrelationId** (and optionally SpanId).
2. Use **agents or SDKs** to send logs to a central backend (Application Insights, Log Analytics, Splunk, ELK, CloudWatch).
3. **Propagate** the same correlation context (e.g. W3C Trace Context) across services so one query returns all related logs.

---

## 9. Sink, enricher, and message template

### Sink

A **sink** is a **destination** for log events. Examples: console, file, SQL Server, Elasticsearch, Application Insights, CloudWatch. A logging library (e.g. Serilog) can write to multiple sinks at once.

### Enricher

An **enricher** adds **extra properties** to every log event (e.g. machine name, environment name, thread ID, TraceId, SpanId). That way you do not repeat the same information in every log call; the pipeline adds it automatically.

### Message template

A **message template** is the first string argument in a log call, with **named placeholders** (e.g. `"Fetching recipe {RecipeId}"`). The placeholders become **structured properties** that backends can index and query. Avoid string interpolation (e.g. `$"Fetching recipe {id}"`) for the main message, as it loses structure.

---

# Part II — Technologies (by category)

## 10. Azure Application Insights

**Application Insights** is Azure’s **application performance management (APM)** and observability service. It collects **telemetry** (requests, dependencies, exceptions, traces, custom events, metrics) from your application and stores it in **Azure Monitor** (in a **Log Analytics workspace**).

### What you get

- **Request performance** — Response times, failure rates, throughput.
- **Dependency tracking** — Outgoing HTTP, database, and queue calls with latency and failures.
- **Exceptions** — With stack traces and context.
- **Custom events and metrics** — Business and operational KPIs.
- **Live metrics** — Near real-time stream (optional).
- **Distributed tracing** — TraceId/SpanId across services when using the same workspace and SDK.

### Data model (high level)

- **Requests** — Incoming HTTP requests.
- **Dependencies** — Outgoing calls.
- **Traces** — Log-like messages (e.g. from ILogger or Serilog).
- **Exceptions** — Exception records.
- **Events** — Custom events.
- **Metrics** — Numeric metrics.

This data lives in the associated **Log Analytics** workspace and is queryable with **KQL** (Kusto Query Language).

---

## 11. Azure Log Analytics

**Log Analytics** is the **log storage and query** engine within **Azure Monitor**. It uses **workspaces** and **Kusto (KQL)** for querying.

### Relationship to Application Insights

- **Application Insights** stores its telemetry in a **Log Analytics workspace**.
- **Log Analytics** also ingests data from many other sources: VMs (agents), diagnostic settings (e.g. storage, Service Bus), custom uploads.

So: **Log Analytics** = storage and query engine; **Application Insights** = application-focused ingestion and UX that writes into that store.

### Common tables (with Application Insights)

- `requests` — Incoming requests.
- `dependencies` — Outgoing dependencies.
- `traces` — Log/trace messages (e.g. from ILogger or Serilog).
- `exceptions` — Exceptions.
- `customEvents` — Custom events.

You use **KQL** in the Log Analytics or Application Insights query editor to filter, aggregate, and join these tables.

### Example KQL (all traces for one operation)

```kusto
traces
| where operation_Id == "your-trace-id"
| order by timestamp asc
```

---

## 12. Splunk

**Splunk** is a commercial **log aggregation and analysis** platform. It ingests logs from files, HTTP, syslog, and other sources, indexes them, and provides search, dashboards, and alerting.

### How it fits

- **Splunk HTTP Event Collector (HEC)** — Applications send JSON logs over HTTP; many logging libraries have a Splunk sink or support generic HTTP.
- **Serilog** — Community sinks (e.g. **Serilog.Sinks.Splunk**) send structured logs to Splunk.
- **Agents** — Splunk Universal Forwarder on the machine can read log files and forward them.

You keep using **ILogger** or **Serilog** in the app; the sink or forwarder sends data to Splunk. Including **CorrelationId** or **TraceId** as structured fields lets you search by request in Splunk.

---

## 13. ELK Stack (Elasticsearch, Logstash, Kibana)

**ELK** is a popular **open-source** stack for log aggregation and search:

| Component | Role |
|-----------|------|
| **Elasticsearch** | Search engine and store for log documents. |
| **Logstash** | Ingests, transforms, and forwards logs (optional; sometimes replaced by Filebeat or direct writes). |
| **Kibana** | UI for search, dashboards, and visualization. |

**Filebeat** is often used as a lightweight shipper that reads log files and sends to Elasticsearch.

### How it fits with .NET

- **Serilog.Sinks.Elasticsearch** — Writes Serilog events directly to Elasticsearch as JSON documents.
- **NLog / ILogger** — Various extensions and sinks can target Elasticsearch or Logstash.
- **OpenTelemetry** — Can export traces/metrics to Elastic APM or Elasticsearch.

Use **structured logs** with **TraceId** and **SpanId** so you can correlate with trace data in Elastic APM or custom dashboards.

---

## 14. AWS CloudWatch

**Amazon CloudWatch** is AWS’s **monitoring and observability** service: **logs**, **metrics**, and (with X-Ray) **tracing**.

### Logs

- **Log groups** and **log streams** — Applications or agents send log events to CloudWatch Logs.
- **CloudWatch Logs Insights** — Query language over log data.

### Metrics

- **Built-in metrics** — EC2, RDS, Lambda, etc.
- **Custom metrics** — Emitted from your app via SDK or agent.

### Tracing

- **AWS X-Ray** — Distributed tracing; integrates with Lambda, ECS, and many AWS services. You can correlate logs with traces by embedding the X-Ray TraceId in log messages.

### How it fits with .NET

- **Serilog.Sinks.AwsCloudWatch** — Serilog sink that writes to CloudWatch Logs.
- **AWS SDK** — Emit custom metrics and log events from .NET.
- **OpenTelemetry** — Export traces to X-Ray via the OpenTelemetry X-Ray exporter.

Including **CorrelationId** or **TraceId** in every log line helps when debugging across services.

---

# Part III — Usage in .NET Core

## 15. ILogger and ASP.NET Core logging

ASP.NET Core uses the **generic `ILogger<T>`** and **`ILoggerFactory`** abstractions. Your code depends on **interfaces**, not a specific implementation, so you can swap providers (Console, Debug, Serilog, Application Insights) without changing application code.

### ILogger&lt;T&gt;

- **T** is usually the class that creates the log (e.g. `ILogger<RecipeController>`). It is used as the **category** so you can filter logs by namespace/class.
- Injected via **dependency injection** in controllers, services, and middleware.

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
| **Message template** | First string parameter with named placeholders (e.g. `"{RecipeId}"`); use this form for structured properties. |
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
// Or use a third-party provider (e.g. Serilog) that replaces or wraps the default pipeline
```

### ASP.NET Core TraceIdentifier

`HttpContext.TraceIdentifier` is a built-in per-request ID. You can expose it in response headers and add it to your logging enrichers so logs are correlated by request.

---

## 16. Serilog with ASP.NET Core

**Serilog** is a popular **structured logging** library for .NET. It provides **sinks** (destinations), **enrichers** (extra properties), and **filters**, and integrates with ASP.NET Core’s **ILogger** so your existing `_logger.LogInformation(...)` code uses Serilog.

### Why Serilog?

- Many **sinks**: file, console, SQL Server, Elasticsearch, Application Insights, etc.
- **Enrichers**: machine name, environment, thread ID, and—with custom or community enrichers—TraceId/SpanId for correlation.
- **Structured properties** from message templates.
- Plugs into the ASP.NET Core logging pipeline so all **ILogger&lt;T&gt;** usage goes through Serilog.

### NuGet packages

- **Serilog.AspNetCore** — Integrates Serilog with the ASP.NET Core host and ILogger (use this in web apps).
- **Serilog.Sinks.Console** — Writes to console (often pulled in via AspNetCore package).
- **Serilog.Sinks.File** — Writes to rolling files.
- **Serilog.Enrichers.Environment** — Adds MachineName, etc.
- **Serilog.Enrichers.Thread** — Adds ThreadId.
- **Serilog.Sinks.ApplicationInsights** — Sends logs to Azure Application Insights (see [§17](#17-serilog--application-insights-in-net-core)).

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

builder.Services.AddSerilog(); // Use Serilog as the ILogger provider

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

Once Serilog is added with `UseSerilog` and `AddSerilog`, **all `ILogger<T>`** usage in the app goes through Serilog. Controllers and services keep using `_logger.LogInformation(...)` and automatically get Serilog’s sinks and enrichers.

### Serilog static Log class

For code without DI (e.g. startup, background jobs), you can use the static **`Log`** class:

```csharp
Log.Information("Application starting");
Log.Error(ex, "Startup failed");
```

For request-scoped data (e.g. correlation ID), use **`LogContext.PushProperty`** so the property appears on all logs created in that scope.

---

## 17. Serilog + Application Insights in .NET Core

You can use **Serilog** for structured logging and **send those logs to Application Insights** so they appear as **traces** (or events) and are queryable in Log Analytics with the same **TraceId** as automatic request and dependency telemetry.

### NuGet

- **Serilog.Sinks.ApplicationInsights** — Converts Serilog `LogEvent` to Application Insights telemetry.

### Use the app’s TelemetryConfiguration

Use the **same `TelemetryConfiguration`** that the app uses (from DI), so sampling and context are consistent:

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

- **TelemetryConverter.Traces** — Serilog events become **TraceTelemetry**; they show up in Application Insights "Traces" and in the Log Analytics `traces` table. This aligns well with request/dependency telemetry for correlation.
- **TelemetryConverter.Events** — Serilog events become **EventTelemetry** (custom events).

Log entries that include an **exception** are sent as **ExceptionTelemetry** regardless of the converter.

### Enrichers for TraceId and correlation

To correlate Serilog logs with Application Insights requests, add **TraceId** and **SpanId** from the current `Activity` to each log:

```csharp
.Enrich.WithProperty("TraceId", () => System.Diagnostics.Activity.Current?.TraceId.ToString())
.Enrich.WithProperty("SpanId", () => System.Diagnostics.Activity.Current?.SpanId.ToString())
```

Then logs sent to Application Insights share the same operation context as the request telemetry.

### ILogger and Serilog together

- Use **Serilog** as the logging provider (`UseSerilog`, `AddSerilog`).
- All **ILogger&lt;T&gt;** calls go through Serilog and can be sent to Application Insights via this sink.
- **ILogger** remains the recommended abstraction in application code; the static `Log` class is only for code without DI.

---

# Part IV — Reference

## 18. Summary and learning sequence

### Terminology at a glance

| Term | Brief summary |
|------|----------------|
| **Observability** | Understanding system state from logs, metrics, and traces. |
| **Logging** | Recording discrete events with levels and optional structure. |
| **Log levels** | Trace, Debug, Information, Warning, Error, Critical. |
| **Structured logging** | Message template + named properties for querying. |
| **Monitoring** | Collecting, storing, visualizing, and alerting on telemetry. |
| **Tracing** | TraceId/SpanId and spans across services. |
| **Telemetry** | All emitted data: logs, metrics, traces, events, exceptions, dependencies. |
| **Correlation ID** | Single ID per request propagated across services and logs. |
| **Distributed logging** | Central collection of logs from many services with correlation. |
| **Sink** | Destination for log events. **Enricher** = adds properties. **Message template** = structured message with placeholders. |

### Technologies at a glance

| Technology | Category | Role |
|------------|----------|------|
| **Application Insights** | Azure | APM; requests, dependencies, traces, exceptions, custom events; data in Log Analytics. |
| **Log Analytics** | Azure | Log store and KQL; backs Application Insights and other sources. |
| **Splunk** | Third-party | Log aggregation and analysis; HEC or Serilog sink. |
| **ELK** | Open-source | Elasticsearch + Logstash/Filebeat + Kibana for log storage and search. |
| **CloudWatch** | AWS | Logs, metrics, and (with X-Ray) tracing. |

### .NET Core usage at a glance

| Topic | Brief summary |
|--------|----------------|
| **ILogger** | .NET’s standard logging abstraction; use in controllers and services with message templates. |
| **Serilog** | Structured logging with sinks and enrichers; plugs into ILogger. |
| **Serilog + App Insights** | Serilog.Sinks.ApplicationInsights with TelemetryConverter.Traces; use DI TelemetryConfiguration and enrichers for TraceId. |

### Suggested learning order

1. **Terminology** — Observability, logging, log levels, structured logging, monitoring, tracing, telemetry, correlation ID, distributed logging, sink, enricher, message template.
2. **Technologies** — Application Insights and Log Analytics; then Splunk, ELK, CloudWatch as alternatives.
3. **ILogger** — ASP.NET Core logging (levels, configuration, message templates, categories).
4. **Serilog** — Sinks, enrichers, integration with ILogger, configuration.
5. **Serilog + Application Insights** — Sink setup, TelemetryConfiguration from DI, TraceId/SpanId enrichers.
6. **Practice** — Add correlation/TraceId, query logs in Log Analytics (KQL), and explore dashboards and alerts.
