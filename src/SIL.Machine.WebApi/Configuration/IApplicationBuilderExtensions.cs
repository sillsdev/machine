namespace Microsoft.AspNetCore.Builder;

public static class IApplicationBuilderExtensions
{
	public static async Task<IApplicationBuilder> UseMachineAsync(this IApplicationBuilder app)
	{
		await app.ApplicationServices.GetService<IRepository<Engine>>()!.InitAsync();
		await app.ApplicationServices.GetService<IRepository<Build>>()!.InitAsync();
		await app.ApplicationServices.GetService<IRepository<DataFile>>()!.InitAsync();
		await app.ApplicationServices.GetService<IRepository<RWLock>>()!.InitAsync();
		await app.ApplicationServices.GetService<IRepository<TrainSegmentPair>>()!.InitAsync();
		await app.ApplicationServices.GetService<IRepository<Webhook>>()!.InitAsync();

		await app.ApplicationServices.GetService<IDistributedReaderWriterLockFactory>()!.InitAsync();
		app.ApplicationServices.GetService<IEngineService>()?.Init();

		return app;
	}
}
