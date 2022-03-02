namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
	public static IMachineBuilder AddMachine(this IServiceCollection services)
	{
		services.AddSingleton<IEngineServiceInternal, EngineService>();
		services.AddSingleton<IEngineService>(sp => sp.GetService<IEngineServiceInternal>()!);
		services.AddSingleton<IEngineRuntimeFactory, SmtTransferEngineRuntime.Factory>();
		services.AddSingleton<IBuildHandler, BuildHandler>();
		services.AddSingleton<IDataFileService, DataFileService>();
		services.AddSingleton<IDistributedReaderWriterLockFactory, DistributedReaderWriterLockFactory>();

		services.Configure<MvcOptions>(o =>
		{
			o.Filters.Add<OperationCancelledExceptionFilter>();
		});
		services.Configure<RouteOptions>(o => o.LowercaseUrls = true);

		var builder = new MachineBuilder(services)
			.AddThotSmtModel()
			.AddTransferEngine()
			.AddTransferTruecaser()
			.AddMemoryDataAccess();
		return builder;
	}
}
