using System.Text.Json;
using System.Text.Json.Serialization;
using SIL.Machine.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
	.AddJsonOptions(o =>
	{
		o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
		o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
	});
builder.Services.AddMachine();
builder.Services.AddSwaggerDocument(doc =>
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
