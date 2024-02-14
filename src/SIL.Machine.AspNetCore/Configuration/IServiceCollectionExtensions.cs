namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IMachineBuilder AddMachine(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (!Sldr.IsInitialized)
            Sldr.Initialize();

        services.AddSingleton<ISharedFileService, SharedFileService>();
        services.AddSingleton<S3HealthCheck>();
        services.AddHealthChecks().AddCheck<S3HealthCheck>("S3 Bucket");

        services.AddSingleton<ILanguageTagService, LanguageTagService>();

        services.AddScoped<IDistributedReaderWriterLockFactory, DistributedReaderWriterLockFactory>();
        services.AddSingleton<ICorpusService, CorpusService>();
        services.AddStartupTask(
            (sp, cancellationToken) =>
                sp.GetRequiredService<IDistributedReaderWriterLockFactory>().InitAsync(cancellationToken)
        );

        var builder = new MachineBuilder(services, configuration);
        if (configuration is null)
        {
            builder.AddServiceOptions(o => { });
            builder.AddSharedFileOptions(o => { });
            builder.AddSmtTransferEngineOptions(o => { });
            builder.AddClearMLOptions(o => { });
        }
        else
        {
            builder.AddServiceOptions(configuration.GetSection(ServiceOptions.Key));
            builder.AddSharedFileOptions(configuration.GetSection(SharedFileOptions.Key));
            builder.AddSmtTransferEngineOptions(configuration.GetSection(SmtTransferEngineOptions.Key));
            builder.AddClearMLOptions(configuration.GetSection(ClearMLOptions.Key));
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
