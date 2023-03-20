namespace Microsoft.Extensions.DependencyInjection;

public interface IMachineConfigurator
{
    IServiceCollection Services { get; }
    IConfiguration? Configuration { get; }
}
