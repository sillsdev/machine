namespace Microsoft.AspNetCore.Builder;

public static class IApplicationBuilderExtensions
{
	public static IApplicationBuilder UseMachine(this IApplicationBuilder app)
	{
		app.ApplicationServices.GetService<IRepository<Engine>>()?.Init();
		app.ApplicationServices.GetService<IRepository<Build>>()?.Init();
		app.ApplicationServices.GetService<IRepository<DataFile>>()?.Init();
		app.ApplicationServices.GetService<IRepository<RWLock>>()?.Init();
		app.ApplicationServices.GetService<IRepository<TrainSegmentPair>>()?.Init();
		app.ApplicationServices.GetService<IRepository<Webhook>>()?.Init();

		app.ApplicationServices.GetService<IEngineService>()?.Init();

		return app;
	}
}
