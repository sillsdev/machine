using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess(builder.Configuration.GetConnectionString("Mongo"))
    .AddMongoBackgroundJobClient(builder.Configuration.GetConnectionString("Hangfire"))
    .AddBackgroundJobServer(builder.Configuration.GetSection("Job:Queues").Get<string[]?>());

var app = builder.Build();

await app.UseMachineAsync();

app.UseHangfireDashboard();

app.Run();
