using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMachine(
    o =>
    {
        o.AddMongoDataAccess();
        o.AddMongoBackgroundJobClient();
        o.AddBackgroundJobServer();
        o.AddServalPlatformService();
    },
    builder.Configuration
);

var app = builder.Build();

app.UseHangfireDashboard();

app.Run();
