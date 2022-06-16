namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IMachineBuilder AddMachine(this IServiceCollection services, IConfiguration? config = null)
    {
        services.AddSingleton<ICorpusService, CorpusService>();
        services.AddSingleton<IDistributedReaderWriterLockFactory, DistributedReaderWriterLockFactory>();
        services
            .AddHttpClient<IWebhookService, WebhookService>()
            .AddTransientHttpErrorPolicy(
                b => b.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            );
        services.AddSingleton<IPretranslationService, PretranslationService>();
        services.AddSingleton<IBuildService, BuildService>();
        services.AddSingleton<IClearMLService, ClearMLService>();
        services.AddSingleton<ISharedFileService, SharedFileService>();

        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        var builder = new MachineBuilder(services)
            .AddOptions(config)
            .AddThotSmtModel()
            .AddTransferEngine()
            .AddUnigramTruecaser()
            .AddMemoryDataAccess();
        return builder;
    }
}
