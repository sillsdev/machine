using Autofac.Extensions.DependencyInjection;
using SIL.Machine.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddMachine();
builder.Services.AddOpenApiDocument(doc =>
{
	doc.Title = "Machine API";
	doc.SchemaNameGenerator = new MachineSchemaNameGenerator();
	doc.UseControllerSummaryAsTagDescription = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseOpenApi();
	app.UseSwaggerUi3();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseMachine();

app.Run();
