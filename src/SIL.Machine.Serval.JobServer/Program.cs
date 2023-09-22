using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess()
    .AddMongoBackgroundJobClient()
    .AddBackgroundJobServer()
    .AddServalPlatformService();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddGrpcClientInstrumentation()
            .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
            .AddConsoleExporter();
    });

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
