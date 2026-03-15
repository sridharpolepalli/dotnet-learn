using ObservabilityDemo.InventoryApi;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();

builder.Host.UseSerilog((context, services, configuration) =>
{
    var cfg = configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "ObservabilityDemo.InventoryApi")
        .Enrich.WithProperty("TraceId", () => System.Diagnostics.Activity.Current?.TraceId.ToString())
        .Enrich.WithProperty("SpanId", () => System.Diagnostics.Activity.Current?.SpanId.ToString())
        .WriteTo.Console();

    var connectionString = context.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(connectionString))
        cfg.WriteTo.ApplicationInsights(connectionString, TelemetryConverter.Traces);
});

builder.Services.AddSerilog();
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});
app.MapControllers();

try
{
    await app.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}
