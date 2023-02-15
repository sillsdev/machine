namespace Microsoft.AspNetCore.Builder;

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseMachine(this IApplicationBuilder app)
    {
        app.UseRepository<TranslationEngine>();
        app.UseRepository<RWLock>();
        app.UseRepository<TrainSegmentPair>();

        app.ApplicationServices.GetService<IDistributedReaderWriterLockFactory>()!.InitAsync().WaitAndUnwrapException();
        app.ApplicationServices.GetService<ITranslationEngineRuntimeService>()?.Init();

        return app;
    }
}
