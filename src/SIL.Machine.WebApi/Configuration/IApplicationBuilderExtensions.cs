namespace Microsoft.AspNetCore.Builder;

public static class IApplicationBuilderExtensions
{
	public static IApplicationBuilder UseMachine(this IApplicationBuilder app)
	{
		app.ApplicationServices.GetService<IRepository<Engine>>().Init();
		app.ApplicationServices.GetService<IBuildRepository>().Init();

		app.ApplicationServices.GetService<IEngineServiceInternal>().Init();

		return app;
	}
}
