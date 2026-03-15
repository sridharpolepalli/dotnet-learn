# .NET Learn — Topic-wise Study Materials

Study materials for .NET: multi-threading, data access, ASP.NET Core, Web API, hosting, observability, and full-stack practice.

Content is organized **by topic** under the **`topics/`** folder.

---

## Repository structure (by topic)

```
topics/
├── concurrency/           # Multi-threading, TPL
├── data-access/           # ADO.NET, sample database
├── aspnet-core/          # Network/HTTP, hosting
├── web-api-rest/         # REST, Recipe API practice
├── observability/        # Logging, monitoring, tracing; ObservabilityDemo
└── fullstack-clients/    # Angular client for Recipe API
```

---

## Topics (by category)

### Concurrency & parallelism

| Topic | Document | Description |
|--------|----------|-------------|
| **Multi-Threading and TPL** | [topics/concurrency/MultiThreading-TPL-Questions.md](./topics/concurrency/MultiThreading-TPL-Questions.md) | Processor, process, thread; ThreadPool; synchronization (Lock, Monitor, Semaphore, Mutex, SpinLock, ReaderWriterLock); TPL, Task, Task&lt;T&gt;; scenario-based Q&A. |

---

### Data access

| Topic | Document | Description |
|--------|----------|-------------|
| **ADO.NET** | [topics/data-access/ADO.NET-Learning-Guide.md](./topics/data-access/ADO.NET-Learning-Guide.md) | Architecture, providers, connectivity, security, transactions, performance. Includes SQL script: [topics/data-access/AdoNetSampleDatabase.sql](./topics/data-access/AdoNetSampleDatabase.sql). |

---

### ASP.NET Core — fundamentals

| Topic | Document | Description |
|--------|----------|-------------|
| **Network, HTTP & Web** | [topics/aspnet-core/ASP.NET-Core-Network-HTTP-Guide.md](./topics/aspnet-core/ASP.NET-Core-Network-HTTP-Guide.md) | Why ASP.NET Core; network/protocols; TCP vs HTTP; HTTP vs HTTPS (SSL/TLS); HTTP methods and concepts; WebApplication builder, configuration, logging. |
| **Hosting** | [topics/aspnet-core/ASP.NET-Core-Hosting-Guide.md](./topics/aspnet-core/ASP.NET-Core-Hosting-Guide.md) | Web server and reverse proxy; hosting options; IIS (hosts, URL Rewrite, Hosting Bundle, web.config); Kestrel; Docker; Azure App Service. |

---

### ASP.NET Web API & REST

| Topic | Document | Description |
|--------|----------|-------------|
| **Web API & REST** | [topics/web-api-rest/ASP.NET-Web-API-REST-Guide.md](./topics/web-api-rest/ASP.NET-Web-API-REST-Guide.md) | Distributed architectures; REST principles; resources, representations, HTTP methods; Recipe API examples; status codes. |
| **Recipe API practice** | [topics/web-api-rest/ASP.NET-Web-API-Recipe-Practice.md](./topics/web-api-rest/ASP.NET-Web-API-Recipe-Practice.md) | Recipe Web API: Onion architecture, Repository (EF Core), DTOs, services, controllers; Swagger; testing (Swagger UI, Postman); .NET Console client. |

---

### Observability (logging, monitoring, tracing)

| Topic | Document | Description |
|--------|----------|-------------|
| **Observability** | [topics/observability/Observability-Logging-Monitoring-Guide.md](./topics/observability/Observability-Logging-Monitoring-Guide.md) | Logging, monitoring, tracing; **ILogger** and ASP.NET Core logging; structured logging; **Serilog** with ASP.NET Core; **correlation ID**; **distributed logging**; **telemetry**; **Azure Application Insights** and **Log Analytics**; **Splunk**, **ELK**, **AWS CloudWatch**; Serilog + Application Insights. |
| **Observability demo** | [topics/observability/ObservabilityDemo/](./topics/observability/ObservabilityDemo/) | Two microservices + Hangfire, correlation ID, Application Insights. See [ObservabilityDemo/README.md](./topics/observability/ObservabilityDemo/README.md) and [ObservabilityDemo/docs/Observability-Demo-Implementation.md](./topics/observability/ObservabilityDemo/docs/Observability-Demo-Implementation.md). |

---

### Full-stack & clients

| Topic | Document | Description |
|--------|----------|-------------|
| **Recipe API — Angular client** | [topics/fullstack-clients/Recipe-API-Angular-Client.md](./topics/fullstack-clients/Recipe-API-Angular-Client.md) | Angular CLI, project structure, environment, models, CategoryService (HttpClient), list/detail/form components, routing; CORS and proxy. |

---

## Quick links (all documents)

- [MultiThreading-TPL-Questions.md](./topics/concurrency/MultiThreading-TPL-Questions.md)
- [ADO.NET-Learning-Guide.md](./topics/data-access/ADO.NET-Learning-Guide.md)
- [AdoNetSampleDatabase.sql](./topics/data-access/AdoNetSampleDatabase.sql)
- [ASP.NET-Core-Network-HTTP-Guide.md](./topics/aspnet-core/ASP.NET-Core-Network-HTTP-Guide.md)
- [ASP.NET-Core-Hosting-Guide.md](./topics/aspnet-core/ASP.NET-Core-Hosting-Guide.md)
- [ASP.NET-Web-API-REST-Guide.md](./topics/web-api-rest/ASP.NET-Web-API-REST-Guide.md)
- [ASP.NET-Web-API-Recipe-Practice.md](./topics/web-api-rest/ASP.NET-Web-API-Recipe-Practice.md)
- [Recipe-API-Angular-Client.md](./topics/fullstack-clients/Recipe-API-Angular-Client.md)
- [Observability-Logging-Monitoring-Guide.md](./topics/observability/Observability-Logging-Monitoring-Guide.md)
- [ObservabilityDemo](./topics/observability/ObservabilityDemo/)
