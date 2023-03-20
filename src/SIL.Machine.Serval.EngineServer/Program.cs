var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMachine(
    o =>
    {
        o.AddMongoDataAccess();
        o.AddMongoBackgroundJobClient();
        o.AddServalTranslationEngineService();
    },
    builder.Configuration
);

var app = builder.Build();

app.MapServalTranslationEngineService();

app.Run();
