var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess(builder.Configuration.GetConnectionString("Mongo"))
    .AddMongoBackgroundJobClient(builder.Configuration.GetConnectionString("Hangfire"))
    .AddServalTranslationService(builder.Configuration.GetConnectionString("Serval"));

var app = builder.Build();

app.UseMachine();
app.MapServalTranslationService();

app.Run();
