using System.IO;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SIL.Machine.WebApi.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var config = new ConfigurationBuilder().AddCommandLine(args).Build();

			var host = new WebHostBuilder()
				.UseKestrel()
				.ConfigureServices(services => services.AddAutofac())
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseConfiguration(config)
				.UseIISIntegration()
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
