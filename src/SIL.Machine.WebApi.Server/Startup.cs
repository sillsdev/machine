using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using SIL.Machine.WebApi.Server.Controllers;
using SIL.Machine.WebApi.Server.DataAccess;
using SIL.Machine.WebApi.Server.Options;
using SIL.Machine.WebApi.Server.Services;

namespace SIL.Machine.WebApi.Server
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

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

			services.AddMvc(o => o.Filters.Add<OperationCancelledExceptionFilter>())
				.AddJsonOptions(a => a.SerializerSettings.ContractResolver
					= new CamelCasePropertyNamesContractResolver());
			services.AddRouting(options => options.LowercaseUrls = true);

			services.Configure<SecurityOptions>(Configuration.GetSection("Security"));

			var smtModel = Configuration.GetValue<string>("SmtModel");
			switch (smtModel)
			{
				case "Thot":
					services.AddThotSmtModel(Configuration);
					break;
			}

			var ruleEngine = Configuration.GetValue<string>("RuleEngine");
			switch (ruleEngine)
			{
				case "Transfer":
					services.AddTransferEngine();
					break;
			}

			var textCorpus = Configuration.GetValue<string>("TextCorpus");
			switch (textCorpus)
			{
				case "XForge":
					services.AddXForgeTextCorpus(Configuration);
					break;

				case "TextFile":
					services.AddTextFileTextCorpus(Configuration);
					break;
			}

			var dataAccess = Configuration.GetValue<string>("DataAccess");
			switch (dataAccess)
			{
				case "Memory":
					services.AddMemoryDataAccess();
					break;

				case "NoDb":
					services.AddNoDbDataAccess(Configuration);
					break;

				case "Mongo":
					services.AddMongoDataAccess(Configuration);
					break;
			}

			services.AddEngineService(Configuration);
		}

		public void ConfigureContainer(ContainerBuilder builder)
		{
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
				app.UseDeveloperExceptionPage();

			app.UseForwardedHeaders(new ForwardedHeadersOptions
			{
				ForwardedHeaders = ForwardedHeaders.XForwardedFor
			});

			app.UseCors("GlobalPolicy");

			app.UseMvc();

			app.UseDataAccess();

			app.UseEngineService();
		}
	}
}
