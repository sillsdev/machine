var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess()
    .AddMongoBackgroundJobClient()
    .AddBackgroundJobServer()
    .AddServalPlatformService();

var app = builder.Build();

app.MapHealthChecks("/health"); //I don't think this really does anything

app.Run();
