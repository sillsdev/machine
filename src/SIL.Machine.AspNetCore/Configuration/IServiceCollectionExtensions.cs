namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddMachine(
        this IServiceCollection services,
        Action<IMachineConfigurator> configure,
        IConfiguration? config = null
    )
    {
        services.AddSingleton<ISharedFileService, SharedFileService>();
        services.AddScoped<IDistributedReaderWriterLockFactory, DistributedReaderWriterLockFactory>();
        services.AddSingleton<ICorpusService, CorpusService>();
        services.AddStartupTask((sp, ct) => sp.GetRequiredService<IDistributedReaderWriterLockFactory>().InitAsync(ct));

        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        var configurator = new MachineConfigurator(services, config);
        if (config is null)
        {
            configurator.AddServiceOptions(o => { });
            configurator.AddSharedFileOptions(o => { });
            configurator.AddSmtTransferEngineOptions(o => { });
            configurator.AddClearMLNmtEngineOptions(o => { });
        }
        else
        {
            configurator.AddServiceOptions(config.GetSection(ServiceOptions.Key));
            configurator.AddSharedFileOptions(config.GetSection(SharedFileOptions.Key));
            configurator.AddSmtTransferEngineOptions(config.GetSection(SmtTransferEngineOptions.Key));
            configurator.AddClearMLNmtEngineOptions(config.GetSection(ClearMLNmtEngineOptions.Key));
        }
        configure(configurator);
        return services;
    }

    public static IServiceCollection AddStartupTask(
        this IServiceCollection services,
        Func<IServiceProvider, CancellationToken, Task> startupTask
    )
    {
        services.AddHostedService(sp => new StartupTask(sp, startupTask));
        return services;
    }
}
