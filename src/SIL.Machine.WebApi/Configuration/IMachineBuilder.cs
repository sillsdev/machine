namespace Microsoft.Extensions.DependencyInjection;

public interface IMachineBuilder
{
	IServiceCollection Services { get; }
}
