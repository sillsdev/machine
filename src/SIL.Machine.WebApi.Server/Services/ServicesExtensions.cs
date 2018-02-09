using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIL.Machine.WebApi.Server.Options;
using SIL.Threading;

namespace SIL.Machine.WebApi.Server.Services
{
	public static class ServicesExtensions
	{
		public static IServiceCollection AddThotSmtModel(this IServiceCollection services, IConfiguration config)
		{
			services.Configure<ThotSmtModelOptions>(config.GetSection("ThotSmtModel"));
			services.AddSingleton<ISmtModelFactory, ThotSmtModelFactory>();
			return services;
		}

		public static IServiceCollection AddTransferEngine(this IServiceCollection services)
		{
			services.AddSingleton<IRuleEngineFactory, TransferEngineFactory>();
			return services;
		}

		public static IServiceCollection AddXForgeTextCorpus(this IServiceCollection services,
			IConfiguration config)
		{
			services.Configure<XForgeTextCorpusOptions>(config.GetSection("XForgeTextCorpus"));
			services.AddSingleton<ITextCorpusFactory, XForgeTextCorpusFactory>();
			return services;
		}

		public static IServiceCollection AddTextFileTextCorpus(this IServiceCollection services, IConfiguration config)
		{
			services.Configure<TextFileTextCorpusOptions>(config.GetSection("TextFileTextCorpus"));
			services.AddSingleton<ITextCorpusFactory, TextFileTextCorpusFactory>();
			return services;
		}

		public static IServiceCollection AddEngineService(this IServiceCollection services, IConfiguration config)
		{
			services.Configure<EngineOptions>(config.GetSection("TranslationEngine"));
			services.AddSingleton<EngineService>();
			services.AddTransient<EngineRunner>();
			return services;
		}

		public static IApplicationBuilder UseEngineService(this IApplicationBuilder app)
		{
			app.ApplicationServices.GetService<EngineService>().InitAsync().WaitAndUnwrapException();
			return app;
		}
	}
}
