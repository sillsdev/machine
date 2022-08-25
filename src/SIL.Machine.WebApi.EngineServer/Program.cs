var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess(builder.Configuration.GetConnectionString("Mongo"))
    .AddMongoBackgroundJobClient(builder.Configuration.GetConnectionString("Hangfire"))
    .AddTranslationEngineServer();

var app = builder.Build();

await app.UseMachineAsync();
app.UseTranslationEngineServer();

app.Run();
