using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIL.Machine.WebApi.Server.Options;

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

		public static IServiceCollection AddShareDBMongoTextCorpus(this IServiceCollection services,
			IConfiguration config)
		{
			services.Configure<ShareDBMongoTextCorpusOptions>(config.GetSection("ShareDBMongoTextCorpus"));
			services.AddSingleton<ITextCorpusFactory, ShareDBMongoTextCorpusFactory>();
			return services;
		}

		public static IServiceCollection AddTextFileTextCorpus(this IServiceCollection services, IConfiguration config)
		{
			services.Configure<TextFileTextCorpusOptions>(config.GetSection("TextFileTextCorpus"));
			services.AddSingleton<ITextCorpusFactory, TextFileTextCorpusFactory>();
			return services;
		}

		public static IApplicationBuilder InitEngineService(this IApplicationBuilder app)
		{
			app.ApplicationServices.GetService<EngineService>().Init();
			return app;
		}
	}
}
