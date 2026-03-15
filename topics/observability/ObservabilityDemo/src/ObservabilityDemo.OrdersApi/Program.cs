using ObservabilityDemo.OrdersApi;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

var builder = WebApplication.CreateBuilder(args);

// ---------- Application Insights (telemetry store: requests, dependencies, traces, exceptions) ----------
builder.Services.AddApplicationInsightsTelemetry();

// ---------- Serilog: structured logging with enrichers, sink to Application Insights ----------
builder.Host.UseSerilog((context, services, configuration) =>
{
    var cfg = configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "ObservabilityDemo.OrdersApi")
        .Enrich.WithProperty("TraceId", () => System.Diagnostics.Activity.Current?.TraceId.ToString())
        .Enrich.WithProperty("SpanId", () => System.Diagnostics.Activity.Current?.SpanId.ToString())
        .WriteTo.Console();

    var connectionString = context.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(connectionString))
        cfg.WriteTo.ApplicationInsights(connectionString, TelemetryConverter.Traces);
});

builder.Services.AddSerilog();

// ---------- HttpClient for InventoryApi (forward correlation ID via DelegatingHandler) ----------
builder.Services.AddHttpClient("InventoryApi", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["InventoryApi:BaseUrl"] ?? "http://localhost:5001");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<CorrelationIdDelegatingHandler>();

// ---------- Hangfire ----------
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddControllers();

var app = builder.Build();

// Correlation ID middleware first so all downstream code sees it (and Serilog enricher gets it).
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions { Authorization = Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>() });
app.MapControllers();

try
{
    await app.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}
