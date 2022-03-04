namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
	public static IMachineBuilder AddMachine(this IServiceCollection services)
	{
		services.AddSingleton<IDataFileService, DataFileService>();
		services.AddSingleton<IDistributedReaderWriterLockFactory, DistributedReaderWriterLockFactory>();

		var builder = new MachineBuilder(services)
			.AddThotSmtModel()
			.AddTransferEngine()
			.AddTransferTruecaser()
			.AddMemoryDataAccess();
		return builder;
	}
}
