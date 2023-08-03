var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess()
    .AddMongoBackgroundJobClient()
    .AddBackgroundJobServer()
    .AddServalPlatformService();

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
