namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IMachineBuilder AddMachine(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton<ISharedFileService, SharedFileService>();
        services.AddScoped<IDistributedReaderWriterLockFactory, DistributedReaderWriterLockFactory>();
        services.AddSingleton<ICorpusService, CorpusService>();
        services.AddStartupTask((sp, ct) => sp.GetRequiredService<IDistributedReaderWriterLockFactory>().InitAsync(ct));

        var builder = new MachineBuilder(services, configuration);
        if (configuration is null)
        {
            builder.AddServiceOptions(o => { });
            builder.AddSharedFileOptions(o => { });
            builder.AddSmtTransferEngineOptions(o => { });
            builder.AddClearMLNmtEngineOptions(o => { });
        }
        else
        {
            builder.AddServiceOptions(configuration.GetSection(ServiceOptions.Key));
            builder.AddSharedFileOptions(configuration.GetSection(SharedFileOptions.Key));
            builder.AddSmtTransferEngineOptions(configuration.GetSection(SmtTransferEngineOptions.Key));
            builder.AddClearMLNmtEngineOptions(configuration.GetSection(ClearMLNmtEngineOptions.Key));
        }
        return builder;
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
