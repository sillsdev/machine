namespace SIL.Machine.AspNetCore.Services;

public static class NmtBuildStages
{
    public const string Preprocess = "preprocess";
    public const string Train = "train";
    public const string Postprocess = "postprocess";
}

public class NmtEngineService(
    IPlatformService platformService,
    IDistributedReaderWriterLockFactory lockFactory,
    IDataAccessContext dataAccessContext,
    IRepository<TranslationEngine> engines,
    IBuildJobService buildJobService,
    ILanguageTagService languageTagService,
    ClearMLMonitorService clearMLMonitorService,
    ISharedFileService sharedFileService
) : ITranslationEngineService
{
    private readonly IDistributedReaderWriterLockFactory _lockFactory = lockFactory;
    private readonly IPlatformService _platformService = platformService;
    private readonly IDataAccessContext _dataAccessContext = dataAccessContext;
    private readonly IRepository<TranslationEngine> _engines = engines;
    private readonly IBuildJobService _buildJobService = buildJobService;
    private readonly ClearMLMonitorService _clearMLMonitorService = clearMLMonitorService;
    private readonly ILanguageTagService _languageTagService = languageTagService;
    private readonly ISharedFileService _sharedFileService = sharedFileService;

    public TranslationEngineType Type => TranslationEngineType.Nmt;

    private const int MinutesToExpire = 60;

    public async Task CreateAsync(
        string engineId,
        string? engineName,
        string sourceLanguage,
        string targetLanguage,
        bool isModelRetrievable = false,
        CancellationToken cancellationToken = default
    )
    {
        await _dataAccessContext.BeginTransactionAsync(cancellationToken);
        await _engines.InsertAsync(
            new TranslationEngine
            {
                EngineId = engineId,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                IsModelRetrievable = isModelRetrievable
            },
            cancellationToken
        );
        await _buildJobService.CreateEngineAsync(
            [BuildJobType.Cpu, BuildJobType.Gpu],
            engineId,
            engineName,
            cancellationToken
        );
        await _dataAccessContext.CommitTransactionAsync(CancellationToken.None);
    }

    public async Task DeleteAsync(string engineId, CancellationToken cancellationToken = default)
    {
        IDistributedReaderWriterLock @lock = await _lockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            await CancelBuildJobAsync(engineId, cancellationToken);

            await _engines.DeleteAsync(e => e.EngineId == engineId, cancellationToken);
            await _buildJobService.DeleteEngineAsync(
                new[] { BuildJobType.Cpu, BuildJobType.Gpu },
                engineId,
                CancellationToken.None
            );
        }
        await _lockFactory.DeleteAsync(engineId, CancellationToken.None);
    }

    public async Task StartBuildAsync(
        string engineId,
        string buildId,
        string? buildOptions,
        IReadOnlyList<Corpus> corpora,
        CancellationToken cancellationToken = default
    )
    {
        IDistributedReaderWriterLock @lock = await _lockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            // If there is a pending/running build, then no need to start a new one.
            if (await _buildJobService.IsEngineBuilding(engineId, cancellationToken))
                throw new InvalidOperationException("The engine is already building or in the process of canceling.");

            await _buildJobService.StartBuildJobAsync(
                BuildJobType.Cpu,
                TranslationEngineType.Nmt,
                engineId,
                buildId,
                NmtBuildStages.Preprocess,
                corpora,
                buildOptions,
                cancellationToken
            );
        }
    }

    public async Task CancelBuildAsync(string engineId, CancellationToken cancellationToken = default)
    {
        IDistributedReaderWriterLock @lock = await _lockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            await CancelBuildJobAsync(engineId, cancellationToken);
        }
    }

    public async Task<ModelPresignedUrl> GetModelPresignedUrlAsync(
        string engineId,
        CancellationToken cancellationToken = default
    )
    {
        TranslationEngine engine = await GetEngineAsync(engineId, cancellationToken);
        if (!engine.IsModelRetrievable)
            throw new InvalidOperationException(
                "The model cannot be downloaded. "
                    + "To enable downloading the model, recreate the engine with IsModelRetrievable property to true."
            );
        if (engine.BuildRevision == 0)
            throw new InvalidOperationException("The engine has not been built yet.");
        string filename = $"{engineId}_{engine.BuildRevision}.tar.gz";
        bool fileExists = await _sharedFileService.ExistsAsync(
            ISharedFileService.ModelDirectory + filename,
            cancellationToken
        );
        if (!fileExists)
            throw new FileNotFoundException(
                $"The model should exist to be downloaded but is not there for BuildRevision {engine.BuildRevision}."
            );
        var modelInfo = new ModelPresignedUrl
        {
            PresignedUrl = (
                await _sharedFileService.GetPresignedUrlAsync(
                    ISharedFileService.ModelDirectory + filename,
                    MinutesToExpire
                )
            ).ToString(),
            BuildRevision = engine.BuildRevision,
            UrlExpirationTime = DateTime
                .UtcNow.AddMinutes(MinutesToExpire)
                .ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz", CultureInfo.InvariantCulture)
        };
        return modelInfo;
    }

    public Task<IReadOnlyList<TranslationResult>> TranslateAsync(
        string engineId,
        int n,
        string segment,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }

    public Task<WordGraph> GetWordGraphAsync(
        string engineId,
        string segment,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }

    public Task TrainSegmentPairAsync(
        string engineId,
        string sourceSegment,
        string targetSegment,
        bool sentenceStart,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }

    public Task<int> GetQueueSizeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_clearMLMonitorService.QueueSize);
    }

    public bool IsLanguageNativeToModel(string language, out string internalCode)
    {
        return _languageTagService.ConvertToFlores200Code(language, out internalCode);
    }

    private async Task CancelBuildJobAsync(string engineId, CancellationToken cancellationToken)
    {
        (string? buildId, BuildJobState jobState) = await _buildJobService.CancelBuildJobAsync(
            engineId,
            cancellationToken
        );
        if (buildId is not null && jobState is BuildJobState.None)
            await _platformService.BuildCanceledAsync(buildId, CancellationToken.None);
    }

    private async Task<TranslationEngine> GetEngineAsync(string engineId, CancellationToken cancellationToken)
    {
        TranslationEngine? engine = await _engines.GetAsync(e => e.EngineId == engineId, cancellationToken);
        if (engine is null)
            throw new InvalidOperationException($"The engine {engineId} does not exist.");
        return engine;
    }
}
