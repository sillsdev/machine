using Microsoft.Extensions.DependencyInjection;

namespace SIL.Machine.WebApi
{
	internal class MachineBuilder : IMachineBuilder
	{
		public MachineBuilder(IServiceCollection services)
		{
			Services = services;
		}

		public IServiceCollection Services { get; }
	}
}
