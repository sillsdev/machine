using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIL.Machine.WebApi.Options;

namespace SIL.Machine.WebApi.Services
{
	public static class ServicesExtensions
	{
		public static IServiceCollection AddThotSmtModel(this IServiceCollection services, IConfigurationRoot config)
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

		public static IServiceCollection AddShareDBMongoTextCorpus(this IServiceCollection services, IConfigurationRoot config)
		{
			services.Configure<ShareDBMongoTextCorpusOptions>(config.GetSection("ShareDBMongoTextCorpus"));
			services.AddSingleton<ITextCorpusFactory, ShareDBMongoTextCorpusFactory>();
			return services;
		}

		public static IServiceCollection AddTextFileTextCorpus(this IServiceCollection services, IConfigurationRoot config)
		{
			services.Configure<TextFileTextCorpusOptions>(config.GetSection("TextFileTextCorpus"));
			services.AddSingleton<ITextCorpusFactory, TextFileTextCorpusFactory>();
			return services;
		}
	}
}
