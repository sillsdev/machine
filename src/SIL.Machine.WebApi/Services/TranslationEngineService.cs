namespace SIL.Machine.WebApi.Services;

public class TranslationEngineService : EntityServiceBase<TranslationEngine>, ITranslationEngineService
{
    private readonly IRepository<Build> _builds;
    private readonly ITranslationEngineRuntimeService _engineRuntimeService;

    public TranslationEngineService(
        IRepository<TranslationEngine> translationEngines,
        IRepository<Build> builds,
        ITranslationEngineRuntimeService engineRuntimeService
    ) : base(translationEngines)
    {
        _builds = builds;
        _engineRuntimeService = engineRuntimeService;
    }

    public async Task<TranslationResult?> TranslateAsync(string engineId, IReadOnlyList<string> segment)
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return null;
        return await _engineRuntimeService.TranslateAsync(engine.Type, engine.Id, segment);
    }

    public async Task<IEnumerable<TranslationResult>?> TranslateAsync(
        string engineId,
        int n,
        IReadOnlyList<string> segment
    )
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return null;
        return await _engineRuntimeService.TranslateAsync(engine.Type, engine.Id, n, segment);
    }

    public async Task<WordGraph?> GetWordGraphAsync(string engineId, IReadOnlyList<string> segment)
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return null;
        return await _engineRuntimeService.GetWordGraphAsync(engine.Type, engine.Id, segment);
    }

    public async Task<bool> TrainSegmentPairAsync(
        string engineId,
        IReadOnlyList<string> sourceSegment,
        IReadOnlyList<string> targetSegment,
        bool sentenceStart
    )
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return false;
        await _engineRuntimeService.TrainSegmentPairAsync(
            engine.Type,
            engine.Id,
            sourceSegment,
            targetSegment,
            sentenceStart
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
        await _engineRuntimeService.CreateAsync(engine.Type, engine.Id);
    }

    public override async Task<bool> DeleteAsync(string engineId)
    {
        CheckDisposed();

        TranslationEngine? engine = await Entities.DeleteAsync(engineId);
        if (engine == null)
            return false;
        await _builds.DeleteAllAsync(b => b.ParentRef == engineId);

        await _engineRuntimeService.DeleteAsync(engine.Type, engine.Id);
        return true;
    }

    public async Task<Build?> StartBuildAsync(string engineId)
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return null;
        return await _engineRuntimeService.StartBuildAsync(engine.Type, engine.Id);
    }

    public async Task CancelBuildAsync(string engineId)
    {
        CheckDisposed();

        TranslationEngine? engine = await GetAsync(engineId);
        if (engine == null)
            return;
        await _engineRuntimeService.CancelBuildAsync(engine.Id);
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
