namespace Microsoft.Extensions.DependencyInjection
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
