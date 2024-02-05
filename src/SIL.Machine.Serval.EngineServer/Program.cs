using Hangfire;
using OpenTelemetry.Trace;
using SIL.Machine.AspNetCore.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder
    .Services.AddMachine(builder.Configuration)
    .AddMongoDataAccess()
    .AddMongoHangfireJobClient()
    .AddServalTranslationEngineService()
    .AddBuildJobService()
    .AddClearMLService();

builder.Services.AddSingleton<ICleanupOldModelsJob, CleanupOldModelsJob>();
RecurringJobOptions options = new() { TimeZone = TimeZoneInfo.Utc };
RecurringJob.AddOrUpdate<ICleanupOldModelsJob>("Cleanup-job", x => x.RunAsync(), Cron.Daily, options);

if (builder.Environment.IsDevelopment())
    builder
        .Services.AddOpenTelemetry()
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

app.UseHttpsRedirection();

app.MapServalTranslationEngineService();
app.MapHangfireDashboard();

app.Run();
