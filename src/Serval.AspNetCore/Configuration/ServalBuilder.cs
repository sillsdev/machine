namespace Microsoft.Extensions.DependencyInjection;

internal class ServalBuilder : IServalBuilder
{
    public ServalBuilder(IServiceCollection services, IConfiguration? configuration)
    {
        Services = services;
        Configuration = configuration;
    }

    public IServiceCollection Services { get; }
    public IConfiguration? Configuration { get; }
}
