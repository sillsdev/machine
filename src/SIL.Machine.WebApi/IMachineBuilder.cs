using Microsoft.Extensions.DependencyInjection;

namespace SIL.Machine.WebApi
{
	public interface IMachineBuilder
	{
		IServiceCollection Services { get; }
	}
}
