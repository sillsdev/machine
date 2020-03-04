using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Serialization;
using SIL.Machine.WebApi.Configuration;
using SIL.Machine.WebApi.Controllers;
using SIL.Machine.WebApi.Services;

namespace Microsoft.Extensions.DependencyInjection
{
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
			services.AddSingleton<IEngineService>(sp => sp.GetService<IEngineServiceInternal>());
			services.AddTransient<EngineRuntime>();
			services.AddSingleton<IBuildHandler, BuildHandler>();

			services.Configure<MvcOptions>(o =>
				{
					o.Filters.Add<OperationCancelledExceptionFilter>();
					o.Conventions.Add(new MachineApplicationModelConvention(config.Namespace,
						config.AuthenticationSchemes));
				});
			services.Configure<MvcNewtonsoftJsonOptions>(o =>
				{
					o.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
				});
			services.Configure<RouteOptions>(o => o.LowercaseUrls = true);

			var builder = new MachineBuilder(services);
			builder.AddThotSmtModel();
			builder.AddTransferEngine();
			builder.AddMemoryDataAccess();
			builder.AddTextFileTextCorpus();
			return builder;
		}
	}
}
