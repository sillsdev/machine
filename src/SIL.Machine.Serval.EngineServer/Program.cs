using Hangfire;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess()
    .AddMongoBackgroundJobClient()
    .AddServalTranslationEngineService();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder.AddAspNetCoreInstrumentation().AddConsoleExporter();
        builder.AddHttpClientInstrumentation().AddConsoleExporter();
        builder.AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources").AddConsoleExporter();
    });

var app = builder.Build();

app.UseHttpsRedirection();

app.MapServalTranslationEngineService();
app.MapGrpcHealthChecksService();
app.MapHangfireDashboard();

app.Run();
