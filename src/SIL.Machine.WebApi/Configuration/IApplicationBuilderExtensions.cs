using Microsoft.Extensions.DependencyInjection;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;

namespace Microsoft.AspNetCore.Builder
{
	public static class IApplicationBuilderExtensions
	{
		public static IApplicationBuilder UseMachine(this IApplicationBuilder app)
		{
			app.ApplicationServices.GetService<IEngineRepository>().Init();
			app.ApplicationServices.GetService<IBuildRepository>().Init();
			app.ApplicationServices.GetService<IRepository<Project>>().Init();

			app.ApplicationServices.GetService<IEngineServiceInternal>().Init();

			return app;
		}
	}
}
