using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess(builder.Configuration.GetConnectionString("Mongo"))
    .AddMongoBackgroundJobClient(builder.Configuration.GetConnectionString("Hangfire"))
    .AddBackgroundJobServer(builder.Configuration.GetSection("Job:Queues").Get<string[]?>())
    .AddServalPlatformService(builder.Configuration.GetConnectionString("Serval"));

var app = builder.Build();

app.UseMachine();

app.UseHangfireDashboard();

app.Run();
