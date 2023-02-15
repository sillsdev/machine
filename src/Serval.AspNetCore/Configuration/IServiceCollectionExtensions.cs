namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServalBuilder AddServal(this IServiceCollection services, IConfiguration? config = null)
    {
        services.AddSingleton<ICorpusService, CorpusService>();
        services
            .AddHttpClient<IWebhookService, WebhookService>()
            .AddTransientHttpErrorPolicy(
                b => b.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            );
        services.AddSingleton<IPretranslationService, PretranslationService>();
        services.AddSingleton<IBuildService, BuildService>();

        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        var builder = new ServalBuilder(services, config).AddOptions();
        return builder;
    }
}
