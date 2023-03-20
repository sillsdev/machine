namespace SIL.Machine.AspNetCore.Services;

public class ClearMLNmtEngineService : TranslationEngineServiceBase<ClearMLNmtEngineBuildJob>
{
    private readonly IClearMLService _clearMLService;

    public ClearMLNmtEngineService(
        IBackgroundJobClient jobClient,
        IPlatformService platformService,
        IDistributedReaderWriterLockFactory lockFactory,
        IDataAccessContext dataAccessContext,
        IRepository<TranslationEngine> engines,
        IClearMLService clearMLService
    )
        : base(jobClient, lockFactory, platformService, dataAccessContext, engines)
    {
        _clearMLService = clearMLService;
    }

    public override TranslationEngineType Type => TranslationEngineType.Nmt;

    public override async Task CreateAsync(
        string engineId,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default
    )
    {
        await base.CreateAsync(engineId, sourceLanguage, targetLanguage, cancellationToken);
        await _clearMLService.CreateProjectAsync(engineId, cancellationToken: CancellationToken.None);
    }

    public override async Task DeleteAsync(string engineId, CancellationToken cancellationToken = default)
    {
        await base.DeleteAsync(engineId, cancellationToken);
        string? projectId = await _clearMLService.GetProjectIdAsync(engineId, CancellationToken.None);
        if (projectId is not null)
            await _clearMLService.DeleteProjectAsync(projectId, CancellationToken.None);
    }

    protected override Expression<Func<ClearMLNmtEngineBuildJob, Task>> GetJobExpression(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> corpora
    )
    {
        return r => r.RunAsync(engineId, buildId, corpora, CancellationToken.None);
    }
}
