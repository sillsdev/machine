using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"job-server.settings.json", optional: false, reloadOnChange: false);
if(builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile($"job-server.settings.Development.json", optional: false, reloadOnChange: false);

builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess(builder.Configuration.GetConnectionString("Mongo"))
    .AddMongoBackgroundJobClient(builder.Configuration.GetConnectionString("Hangfire"))
    .AddBackgroundJobServer(builder.Configuration.GetSection("Job:Queues").Get<string[]?>());

var app = builder.Build();

await app.UseMachineAsync();

app.UseHangfireDashboard();

app.Run();
