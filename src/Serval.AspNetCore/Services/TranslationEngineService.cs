using Serval.Engine.Translation.V1;

namespace Serval.AspNetCore.Services;

public class TranslationEngineService : EntityServiceBase<TranslationEngine>, ITranslationEngineService
{
    private readonly IRepository<Build> _builds;
    private readonly GrpcClientFactory _grpcClientFactory;

    public TranslationEngineService(
        IRepository<TranslationEngine> translationEngines,
        IRepository<Build> builds,
        GrpcClientFactory grpcClientFactory
    ) : base(translationEngines)
    {
        _builds = builds;
        _grpcClientFactory = grpcClientFactory;
    }

    public async Task<TranslationResult?> TranslateAsync(string engineId, string segment)
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return null;
        var client = _grpcClientFactory.CreateClient<TranslationService.TranslationServiceClient>(engine.Type);
        TranslateResponse response = await client.TranslateAsync(
            new TranslateRequest
            {
                EngineType = engine.Type,
                EngineId = engine.Id,
                N = 1,
                Segment = segment
            }
        );
        return response.Results[0];
    }

    public async Task<IEnumerable<TranslationResult>?> TranslateAsync(string engineId, int n, string segment)
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return null;
        var client = _grpcClientFactory.CreateClient<TranslationService.TranslationServiceClient>(engine.Type);
        TranslateResponse response = await client.TranslateAsync(
            new TranslateRequest
            {
                EngineType = engine.Type,
                EngineId = engine.Id,
                N = n,
                Segment = segment
            }
        );
        return response.Results;
    }

    public async Task<WordGraph?> GetWordGraphAsync(string engineId, string segment)
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return null;
        var client = _grpcClientFactory.CreateClient<TranslationService.TranslationServiceClient>(engine.Type);
        GetWordGraphResponse response = await client.GetWordGraphAsync(
            new GetWordGraphRequest
            {
                EngineType = engine.Type,
                EngineId = engine.Id,
                Segment = segment
            }
        );
        return response.WordGraph;
    }

    public async Task<bool> TrainSegmentPairAsync(
        string engineId,
        string sourceSegment,
        string targetSegment,
        bool sentenceStart
    )
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return false;
        var client = _grpcClientFactory.CreateClient<TranslationService.TranslationServiceClient>(engine.Type);
        await client.TrainSegmentPairAsync(
            new TrainSegmentPairRequest
            {
                EngineType = engine.Type,
                EngineId = engine.Id,
                SourceSegment = sourceSegment,
                TargetSegment = targetSegment,
                SentenceStart = sentenceStart
            }
        );
        return true;
    }

    public async Task<IEnumerable<TranslationEngine>> GetAllAsync(string owner)
    {
        CheckDisposed();

        return await Entities.GetAllAsync(e => e.Owner == owner);
    }

    public override async Task CreateAsync(TranslationEngine engine)
    {
        CheckDisposed();

        await Entities.InsertAsync(engine);
        var client = _grpcClientFactory.CreateClient<TranslationService.TranslationServiceClient>(engine.Type);
        await client.CreateAsync(new CreateRequest { EngineType = engine.Type, EngineId = engine.Id });
    }

    public override async Task<bool> DeleteAsync(string engineId)
    {
        CheckDisposed();

        TranslationEngine? engine = await Entities.DeleteAsync(engineId);
        if (engine == null)
            return false;
        await _builds.DeleteAllAsync(b => b.ParentRef == engineId);

        var client = _grpcClientFactory.CreateClient<TranslationService.TranslationServiceClient>(engine.Type);
        await client.DeleteAsync(new DeleteRequest { EngineType = engine.Type, EngineId = engine.Id });
        return true;
    }

    public async Task<Build?> StartBuildAsync(string engineId)
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return null;

        var build = new Build { ParentRef = engine.Id };
        await _builds.InsertAsync(build);

        var client = _grpcClientFactory.CreateClient<TranslationService.TranslationServiceClient>(engine.Type);
        await client.StartBuildAsync(
            new StartBuildRequest
            {
                EngineType = engine.Type,
                EngineId = engine.Id,
                BuildId = build.Id
            }
        );
        return build;
    }

    public async Task CancelBuildAsync(string engineId)
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return;
        var client = _grpcClientFactory.CreateClient<TranslationService.TranslationServiceClient>(engine.Type);
        await client.CancelBuildAsync(new CancelBuildRequest { EngineType = engine.Type, EngineId = engine.Id });
    }

    public Task AddCorpusAsync(string engineId, TranslationEngineCorpus corpus)
    {
        CheckDisposed();

        return Entities.UpdateAsync(engineId, u => u.Add(e => e.Corpora, corpus));
    }

    public async Task<bool> DeleteCorpusAsync(string engineId, string corpusId)
    {
        CheckDisposed();

        TranslationEngine? engine = await Entities.UpdateAsync(
            engineId,
            u => u.RemoveAll(e => e.Corpora, c => c.CorpusRef == corpusId)
        );
        return engine is not null;
    }
}
