namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
	public static IMachineBuilder AddMachine(this IServiceCollection services)
	{
		return services.AddMachine(o => { });
	}

	public static IMachineBuilder AddMachine(this IServiceCollection services,
		Action<MachineConfig> setupAction)
	{
		var config = new MachineConfig();
		setupAction(config);
		services.AddSingleton<IEngineServiceInternal, EngineService>();
		services.AddSingleton<IEngineService>(sp => sp.GetService<IEngineServiceInternal>()!);
		services.AddSingleton<IEngineRuntimeFactory, SmtTransferEngineRuntime.Factory>();
		services.AddSingleton<IBuildHandler, BuildHandler>();
		services.AddSingleton<IDataFileService, DataFileService>();

		services.Configure<MvcOptions>(o =>
		{
			o.Filters.Add<OperationCancelledExceptionFilter>();
			//o.Conventions.Add(new MachineApplicationModelConvention(config.Namespace,
			//	config.AuthenticationSchemes));
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
