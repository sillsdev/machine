using System.Security.Claims;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;
using SIL.Machine.WebApi.Controllers;
using SIL.Machine.WebApi.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHangfire(c => c
	.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
	.UseSimpleAssemblyNameTypeSerializer()
	.UseRecommendedSerializerSettings()
	.UseMongoStorage(builder.Configuration.GetConnectionString("Hangfire"), new MongoStorageOptions
	{
		MigrationOptions = new MongoMigrationOptions
		{
			MigrationStrategy = new MigrateMongoMigrationStrategy(),
			BackupStrategy = new CollectionMongoBackupStrategy()
		},
		CheckConnection = true,
		CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
	}));
builder.Services.AddHangfireServer();

builder.Services.AddControllers()
	.AddJsonOptions(o =>
	{
		o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
		o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
	});

string authority = $"https://{builder.Configuration["Auth:Domain"]}/";
builder.Services.AddAuthentication(o =>
{
	o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
	o.Authority = authority;
	o.Audience = builder.Configuration["Auth:Audience"];
	o.TokenValidationParameters = new TokenValidationParameters
	{
		NameClaimType = ClaimTypes.NameIdentifier
	};
});

builder.Services.AddAuthorization(o =>
{
	foreach (string scope in Scopes.All)
		o.AddPolicy(scope, policy => policy.Requirements.Add(new HasScopeRequirement(scope, authority)));
	o.AddPolicy("IsOwner", policy => policy.Requirements.Add(new IsOwnerRequirement()));
});
builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, IsEngineOwnerHandler>();

builder.Services.AddMachine()
	.AddMongoDataAccess(builder.Configuration.GetConnectionString("Mongo"))
	.AddEngineOptions(builder.Configuration.GetSection("Engine"))
	.AddServiceOptions(builder.Configuration.GetSection("Service"));

builder.Services.AddSwaggerDocument(doc =>
{
	doc.Title = "Machine API";
	doc.SchemaNameGenerator = new MachineSchemaNameGenerator();
	doc.UseControllerSummaryAsTagDescription = true;
	doc.AddSecurity("bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
	{
		Type = OpenApiSecuritySchemeType.OAuth2,
		Description = "Auth0 Client Credentials Flow",
		Flow = OpenApiOAuth2Flow.Application,
		Flows = new OpenApiOAuthFlows
		{
			ClientCredentials = new OpenApiOAuthFlow
			{
				AuthorizationUrl = $"{authority}authorize",
				TokenUrl = $"{authority}oauth/token"
			}
		},

	});
	doc.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("bearer"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseOpenApi();
app.UseSwaggerUi3(settings =>
{
	settings.OAuth2Client = new OAuth2ClientSettings
	{
		AppName = "Auth0 M2M App",
		AdditionalQueryStringParameters =
		{
			{ "audience", builder.Configuration["Auth:Audience"] }
		}
	};
	if (app.Environment.IsDevelopment())
	{
		settings.OAuth2Client.ClientId = builder.Configuration["TestClientId"];
		settings.OAuth2Client.ClientSecret = builder.Configuration["TestClientSecret"];
	}

	settings.CustomJavaScriptPath = "js/auth0.js";
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseMachine();

app.Run();
