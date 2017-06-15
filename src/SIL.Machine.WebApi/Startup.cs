using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Options;
using SIL.Machine.WebApi.Services;

namespace SIL.Machine.WebApi
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddJsonFile("appsettings.user.json", optional: true)
				.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			// Add framework services.
			services.AddCors(options =>
				{
					options.AddPolicy("GlobalPolicy", policy => policy
						.AllowAnyOrigin()
						.AllowAnyMethod()
						.AllowAnyHeader()
						.AllowCredentials());
				});

			services.AddMvc()
				.AddJsonOptions(a => a.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver());

			services.Configure<EngineOptions>(Configuration.GetSection("TranslationEngine"));
			services.Configure<SecurityOptions>(Configuration.GetSection("Security"));

			IConfigurationSection smtModelConfig = Configuration.GetSection("SmtModel");
			switch (smtModelConfig.GetValue<string>("Type"))
			{
				case "Thot":
					services.Configure<ThotSmtModelOptions>(smtModelConfig);
					services.AddSingleton<ISmtModelFactory, ThotSmtModelFactory>();
					break;
			}

			IConfigurationSection ruleEngineConfig = Configuration.GetSection("RuleEngine");
			switch (ruleEngineConfig.GetValue<string>("Type"))
			{
				case "Transfer":
					services.AddSingleton<IRuleEngineFactory, TransferEngineFactory>();
					break;
			}

			IConfigurationSection textCorpusConfig = Configuration.GetSection("TextCorpus");
			switch (textCorpusConfig.GetValue<string>("Type"))
			{
				case "ShareDBMongo":
					services.Configure<ShareDBMongoTextCorpusOptions>(textCorpusConfig);
					services.AddSingleton<ITextCorpusFactory, ShareDBMongoTextCorpusFactory>();
					break;

				case "TextFile":
					services.Configure<TextFileTextCorpusOptions>(textCorpusConfig);
					services.AddSingleton<ITextCorpusFactory, TextFileTextCorpusFactory>();
					break;
			}

			services.AddSingleton<IEngineRepository, MemoryEngineRepository>();
			services.AddSingleton<IBuildRepository, MemoryBuildRepository>();
			services.AddSingleton<EngineService>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			app.UseForwardedHeaders(new ForwardedHeadersOptions
				{
					ForwardedHeaders = ForwardedHeaders.XForwardedFor
				});

			app.UseCors("GlobalPolicy");

			app.UseMvc();
		}
	}
}
