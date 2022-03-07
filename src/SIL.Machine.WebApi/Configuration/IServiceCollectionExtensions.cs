namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
	public static IMachineBuilder AddMachine(this IServiceCollection services)
	{
		services.AddSingleton<IDataFileService, DataFileService>();
		services.AddSingleton<IDistributedReaderWriterLockFactory, DistributedReaderWriterLockFactory>();
		services.AddHttpClient<IWebhookService, WebhookService>()
			.AddTransientHttpErrorPolicy(b =>
				b.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

		services.AddAutoMapper(Assembly.GetExecutingAssembly());

		var builder = new MachineBuilder(services)
			.AddThotSmtModel()
			.AddTransferEngine()
			.AddTransferTruecaser()
			.AddMemoryDataAccess();
		return builder;
	}
}
