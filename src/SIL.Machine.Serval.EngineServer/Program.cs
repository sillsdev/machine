using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess()
    .AddMongoBackgroundJobClient()
    .AddServalTranslationEngineService();

builder.Services.AddHealthChecks().AddCheck<S3HealthCheck>("S3 Bucket");

var app = builder.Build();

app.UseHttpsRedirection();

app.MapServalTranslationEngineService();
app.MapGrpcHealthChecksService();
app.MapHangfireDashboard();

app.Run();
