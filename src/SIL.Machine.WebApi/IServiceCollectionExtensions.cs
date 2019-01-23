using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SIL.Machine.WebApi.Controllers;
using SIL.Machine.WebApi.Options;
using SIL.Machine.WebApi.Services;

namespace SIL.Machine.WebApi
{
	public static class IServiceCollectionExtensions
	{
		public static IMachineBuilder AddMachine(this IServiceCollection services)
		{
			return services.AddMachine(o => { });
		}

		public static IMachineBuilder AddMachine(this IServiceCollection services,
			Action<MachineOptions> configureOptions)
		{
			var options = new MachineOptions();
			configureOptions(options);
			services.ConfigureOptions(options);
			services.AddSingleton<EngineService>();
			services.AddTransient<EngineRuntime>();

			services.Configure<MvcOptions>(o =>
				{
					o.Filters.Add<OperationCancelledExceptionFilter>();
					o.Conventions.Add(new MachineApplicationModelConvention(options.Namespace,
						options.AuthenticationSchemes));
				});
			services.Configure<MvcJsonOptions>(o =>
				{
					o.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
					o.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
				});

			var builder = new MachineBuilder(services);
			builder.AddThotSmtModel();
			builder.AddTransferEngine();
			builder.AddMemoryDataAccess();
			builder.AddTextFileTextCorpus();
			return builder;
		}
	}
}
