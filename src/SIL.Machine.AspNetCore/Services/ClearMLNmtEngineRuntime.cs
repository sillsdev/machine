namespace SIL.Machine.AspNetCore.Services;

public class ClearMLNmtEngineRuntime : TranslationEngineRuntimeBase<ClearMLNmtEngineBuildJob>
{
    public class Factory : TranslationEngineRuntimeFactory<ClearMLNmtEngineRuntime>
    {
        public Factory(IServiceProvider serviceProvider) : base(serviceProvider, TranslationEngineType.Nmt) { }
    }

    private readonly IPlatformService _platformService;
    private readonly IClearMLService _clearMLService;

    public ClearMLNmtEngineRuntime(
        IPlatformService platformService,
        IClearMLService clearMLService,
        IBackgroundJobClient jobClient,
        IDistributedReaderWriterLockFactory lockFactory,
        IRepository<TranslationEngine> engines,
        string engineId
    ) : base(jobClient, lockFactory, engines, engineId)
    {
        _platformService = platformService;
        _clearMLService = clearMLService;
    }

    public override async Task InitNewAsync()
    {
        CheckDisposed();

        await base.InitNewAsync();

        TranslationEngineInfo? engineInfo = await _platformService.GetTranslationEngineInfoAsync(EngineId);
        if (engineInfo is null)
            throw new InvalidOperationException("The host translation engine does not exist.");
        await _clearMLService.CreateProjectAsync(EngineId, engineInfo.Name);
    }

    public override async Task DeleteDataAsync()
    {
        CheckDisposed();

        string? projectId = await _clearMLService.GetProjectIdAsync(EngineId);
        if (projectId is null)
            return;

        await _clearMLService.DeleteProjectAsync(projectId);

        await base.DeleteDataAsync();
    }

    protected override Expression<Func<ClearMLNmtEngineBuildJob, Task>> GetJobExpression(string buildId)
    {
        return r => r.RunAsync(EngineId, buildId, CancellationToken.None);
    }
}
