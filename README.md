# Jan2026

Study materials for multi-threading and parallel programming in .NET.

## Topics

**[Multi-Threading and TPL: Questions & Answers Guide](./MultiThreading-TPL-Questions.md)**

A guide covering:

- Processor, Process, and Thread fundamentals
- Thread class, ThreadStart, ParameterizedThreadStart
- ThreadPool and QueueUserWorkItem
- Foreground vs Background threads
- Synchronization and lock types (Lock, Monitor, Semaphore, Mutex, SpinLock, ReaderWriterLock)
- TPL, Task, and Task&lt;T&gt;
- Scenario-based questions with solutions

**[ADO.NET: Professional Learning Guide](./ADO.NET-Learning-Guide.md)** — Architecture, providers, connectivity, security, transactions, performance. Includes SQL setup script ([`docs/ado-net/AdoNetSampleDatabase.sql`](./docs/ado-net/AdoNetSampleDatabase.sql)) and copy-ready C# examples.

**[ASP.NET Core — Network, HTTP & Web Fundamentals](./ASP.NET-Core-Network-HTTP-Guide.md)** — Why ASP.NET Core, network programming and protocols, TCP vs HTTP, HTTP vs HTTPS (SSL/TLS handshake, asymmetric vs symmetric encryption), and HTTP in detail (methods and important concepts).

**[ASP.NET Core — Hosting Guide](./ASP.NET-Core-Hosting-Guide.md)** — Web server and reverse proxy concepts, ASP.NET Core hosting options, step-by-step: create MVC app, deploy to local IIS (hosts file, IIS features, URL Rewrite, Hosting Bundle, web.config, in-process vs out-of-process), Kestrel-only hosting, Docker (install and host ASP.NET Core), Azure App Service (Visual Studio and dotnet/AZ CLI).

**[ASP.NET Web API & REST](./ASP.NET-Web-API-REST-Guide.md)** — Evolution of distributed architectures, introduction to REST, key principles (stateless, client-server, uniform interface), resources and representations, HTTP methods, example requests and responses for a Recipe API, and HTTP status code reference.

**[ASP.NET Web API — Recipe Practice (Onion Architecture)](./ASP.NET-Web-API-Recipe-Practice.md)** — Step-by-step: Recipe Web API with Repository layer (EF Core, Repository pattern), DTOs, Application layer (services), Controllers; enable Swagger in Visual Studio; testing with Swagger UI, Postman, and other tools; sample .NET Console client.

**[Recipe API — Angular Client (Step-by-Step)](./Recipe-API-Angular-Client.md)** — Detailed setup, implement, and run: Angular CLI, project structure, environment (API URL), models, CategoryService (HttpClient), category list/detail/form components, routing, run and test; optional CORS and proxy.
