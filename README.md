# .NET Learn — Topic-wise Study Materials

Study materials for .NET: multi-threading, data access, ASP.NET Core, Web API, hosting, observability, and full-stack practice.

---

## Topics (by category)

### Concurrency & parallelism

| Topic | Document | Description |
|--------|----------|-------------|
| **Multi-Threading and TPL** | [MultiThreading-TPL-Questions.md](./MultiThreading-TPL-Questions.md) | Processor, process, thread; ThreadPool; synchronization (Lock, Monitor, Semaphore, Mutex, SpinLock, ReaderWriterLock); TPL, Task, Task&lt;T&gt;; scenario-based Q&A. |

---

### Data access

| Topic | Document | Description |
|--------|----------|-------------|
| **ADO.NET** | [ADO.NET-Learning-Guide.md](./ADO.NET-Learning-Guide.md) | Architecture, providers, connectivity, security, transactions, performance. Includes SQL script: [docs/ado-net/AdoNetSampleDatabase.sql](./docs/ado-net/AdoNetSampleDatabase.sql). |

---

### ASP.NET Core — fundamentals

| Topic | Document | Description |
|--------|----------|-------------|
| **Network, HTTP & Web** | [ASP.NET-Core-Network-HTTP-Guide.md](./ASP.NET-Core-Network-HTTP-Guide.md) | Why ASP.NET Core; network/protocols; TCP vs HTTP; HTTP vs HTTPS (SSL/TLS); HTTP methods and concepts; WebApplication builder, configuration, logging. |
| **Hosting** | [ASP.NET-Core-Hosting-Guide.md](./ASP.NET-Core-Hosting-Guide.md) | Web server and reverse proxy; hosting options; IIS (hosts, URL Rewrite, Hosting Bundle, web.config); Kestrel; Docker; Azure App Service. |

---

### ASP.NET Web API & REST

| Topic | Document | Description |
|--------|----------|-------------|
| **Web API & REST** | [ASP.NET-Web-API-REST-Guide.md](./ASP.NET-Web-API-REST-Guide.md) | Distributed architectures; REST principles; resources, representations, HTTP methods; Recipe API examples; status codes. |
| **Recipe API practice** | [ASP.NET-Web-API-Recipe-Practice.md](./ASP.NET-Web-API-Recipe-Practice.md) | Recipe Web API: Onion architecture, Repository (EF Core), DTOs, services, controllers; Swagger; testing (Swagger UI, Postman); .NET Console client. |

---

### Observability (logging, monitoring, tracing)

| Topic | Document | Description |
|--------|----------|-------------|
| **Observability** | [docs/Observability-Logging-Monitoring-Guide.md](./docs/Observability-Logging-Monitoring-Guide.md) | Logging, monitoring, tracing; **ILogger** and ASP.NET Core logging; structured logging; **Serilog** with ASP.NET Core; **correlation ID**; **distributed logging**; **telemetry**; **Azure Application Insights** and **Log Analytics**; **Splunk**, **ELK**, **AWS CloudWatch**; Serilog + Application Insights. |

---

### Full-stack & clients

| Topic | Document | Description |
|--------|----------|-------------|
| **Recipe API — Angular client** | [Recipe-API-Angular-Client.md](./Recipe-API-Angular-Client.md) | Angular CLI, project structure, environment, models, CategoryService (HttpClient), list/detail/form components, routing; CORS and proxy. |

---

## Quick links (all documents)

- [MultiThreading-TPL-Questions.md](./MultiThreading-TPL-Questions.md)
- [ADO.NET-Learning-Guide.md](./ADO.NET-Learning-Guide.md)
- [ASP.NET-Core-Network-HTTP-Guide.md](./ASP.NET-Core-Network-HTTP-Guide.md)
- [ASP.NET-Core-Hosting-Guide.md](./ASP.NET-Core-Hosting-Guide.md)
- [ASP.NET-Web-API-REST-Guide.md](./ASP.NET-Web-API-REST-Guide.md)
- [ASP.NET-Web-API-Recipe-Practice.md](./ASP.NET-Web-API-Recipe-Practice.md)
- [Recipe-API-Angular-Client.md](./Recipe-API-Angular-Client.md)
- [docs/Observability-Logging-Monitoring-Guide.md](./docs/Observability-Logging-Monitoring-Guide.md)
- [docs/ado-net/AdoNetSampleDatabase.sql](./docs/ado-net/AdoNetSampleDatabase.sql)
