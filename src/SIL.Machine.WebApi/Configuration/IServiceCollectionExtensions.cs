namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
	public static IMachineBuilder AddMachine(this IServiceCollection services)
	{
		services.AddSingleton<ICorpusService, CorpusService>();
		services.AddSingleton<IDistributedReaderWriterLockFactory, DistributedReaderWriterLockFactory>();
		services.AddHttpClient<IWebhookService, WebhookService>()
			.AddTransientHttpErrorPolicy(b =>
				b.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
		services.AddSingleton<IPretranslationService, PretranslationService>();
		services.AddSingleton<IBuildService, BuildService>();

		services.AddAutoMapper(Assembly.GetExecutingAssembly());

		var builder = new MachineBuilder(services)
			.AddTranslationEngineOptions(o => { })
			.AddCorpusOptions(o => { })
			.AddApiOptions(o => { })
			.AddThotSmtModel()
			.AddTransferEngine()
			.AddUnigramTruecaser()
			.AddMemoryDataAccess();
		return builder;
	}
}
