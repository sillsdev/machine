namespace Microsoft.Extensions.DependencyInjection;

internal class MachineBuilder : IMachineBuilder
{
    public MachineBuilder(IServiceCollection services, IConfiguration? configuration)
    {
        Services = services;
        Configuration = configuration;
    }

    public IServiceCollection Services { get; }
    public IConfiguration? Configuration { get; }
}
