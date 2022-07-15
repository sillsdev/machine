using System.Security.Claims;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;
using SIL.Machine.WebApi.ApiServer;
using SIL.Machine.WebApi.Controllers;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"machine-api.settings.json", optional: false, reloadOnChange: false);
if(builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile($"machine-api.settings.Development.json", optional: false, reloadOnChange: false);

// Add services to the container.

builder.Services.AddRouting(o => o.LowercaseUrls = true);

builder.Services
    .AddControllers()
    .AddJsonOptions(
        o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        }
    );

string authority = $"https://{builder.Configuration["Auth:Domain"]}/";
builder.Services
    .AddAuthentication(
        o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }
    )
    .AddJwtBearer(
        o =>
        {
            o.Authority = authority;
            o.Audience = builder.Configuration["Auth:Audience"];
            o.TokenValidationParameters = new TokenValidationParameters { NameClaimType = ClaimTypes.NameIdentifier };
        }
    );

builder.Services.AddAuthorization(
    o =>
    {
        foreach (string scope in Scopes.All)
            o.AddPolicy(scope, policy => policy.Requirements.Add(new HasScopeRequirement(scope, authority)));
        o.AddPolicy("IsOwner", policy => policy.Requirements.Add(new IsOwnerRequirement()));
    }
);
builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, IsEntityOwnerHandler>();

var machineBuilder = builder.Services
    .AddMachine(builder.Configuration)
    .AddMongoDataAccess(builder.Configuration.GetConnectionString("Mongo"))
    .AddMongoBackgroundJobClient(builder.Configuration.GetConnectionString("Hangfire"))
    .AddTranslationEngineService();

if (builder.Environment.IsDevelopment())
    machineBuilder.AddBackgroundJobServer(builder.Configuration.GetSection("Job:Queues").Get<string[]?>());

builder.Services.AddSwaggerDocument(
    doc =>
    {
        doc.Title = "Machine API";
        doc.SchemaNameGenerator = new MachineSchemaNameGenerator();
        doc.UseControllerSummaryAsTagDescription = true;
        doc.AddSecurity(
            "bearer",
            Enumerable.Empty<string>(),
            new OpenApiSecurityScheme
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
            }
        );
        doc.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("bearer"));
    }
);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseOpenApi();
app.UseSwaggerUi3(
    settings =>
    {
        settings.OAuth2Client = new OAuth2ClientSettings
        {
            AppName = "Auth0 M2M App",
            AdditionalQueryStringParameters = { { "audience", builder.Configuration["Auth:Audience"] } }
        };
        if (app.Environment.IsDevelopment())
        {
            settings.OAuth2Client.ClientId = builder.Configuration["TestClientId"];
            settings.OAuth2Client.ClientSecret = builder.Configuration["TestClientSecret"];
        }

        settings.CustomJavaScriptPath = "js/auth0.js";
    }
);

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.UseMachineAsync();

if (builder.Environment.IsDevelopment())
{
    app.UseHangfireDashboard();
}

app.Run();
