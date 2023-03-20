namespace Microsoft.Extensions.DependencyInjection;

internal class MachineConfigurator : IMachineConfigurator
{
    public MachineConfigurator(IServiceCollection services, IConfiguration? configuration)
    {
        Services = services;
        Configuration = configuration;
    }

    public IServiceCollection Services { get; }
    public IConfiguration? Configuration { get; }
}
