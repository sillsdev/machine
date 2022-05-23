using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMachine()
    .AddMongoDataAccess(builder.Configuration.GetConnectionString("Mongo"))
    .AddEngineOptions(builder.Configuration.GetSection("Engine"))
    .AddServiceOptions(builder.Configuration.GetSection("Service"))
    .AddMongoBackgroundJobClient(builder.Configuration.GetConnectionString("Hangfire"))
    .AddBackgroundJobServer();

var app = builder.Build();

await app.UseMachineAsync();

app.UseHangfireDashboard();

app.Run();
