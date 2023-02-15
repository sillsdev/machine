namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IMachineBuilder AddMachine(this IServiceCollection services, IConfiguration? config = null)
    {
        services.AddSingleton<IClearMLService, ClearMLService>();
        services.AddSingleton<ISharedFileService, SharedFileService>();
        services.AddSingleton<IDistributedReaderWriterLockFactory, DistributedReaderWriterLockFactory>();

        var builder = new MachineBuilder(services, config).AddOptions();
        return builder;
    }
}
