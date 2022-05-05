using System.Runtime.InteropServices;
using Hangfire;
using Python.Deployment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMachine()
	.AddMongoDataAccess(builder.Configuration.GetConnectionString("Mongo"))
	.AddTranslationEngineOptions(builder.Configuration.GetSection("TranslationEngine"))
	.AddServiceOptions(builder.Configuration.GetSection("Service"))
	.AddCorpusOptions(builder.Configuration.GetSection("Corpus"))
	.AddMongoBackgroundJobClient(builder.Configuration.GetConnectionString("Hangfire"))
	.AddBackgroundJobServer();

var app = builder.Build();

await app.UseMachineAsync();

if (builder.Environment.IsDevelopment() && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
	await Installer.SetupPython();
	Installer.PipInstallModule("sil-machine");
}

app.UseHangfireDashboard();

app.Run();
