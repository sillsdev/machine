using Serval.Engine.Translation.V1;

namespace Serval.AspNetCore.Services;

public interface ITranslationEngineService
{
    Task<IEnumerable<TranslationEngine>> GetAllAsync(string owner);
    Task<TranslationEngine?> GetAsync(string engineId, CancellationToken cancellationToken = default);

    Task CreateAsync(TranslationEngine engine);
    Task<bool> DeleteAsync(string engineId);

    Task<TranslationResult?> TranslateAsync(string engineId, string segment);

    Task<IEnumerable<TranslationResult>?> TranslateAsync(string engineId, int n, string segment);

    Task<WordGraph?> GetWordGraphAsync(string engineId, string segment);

    Task<bool> TrainSegmentPairAsync(string engineId, string sourceSegment, string targetSegment, bool sentenceStart);

    Task<Build?> StartBuildAsync(string engineId);

    Task CancelBuildAsync(string engineId);

    Task AddCorpusAsync(string engineId, TranslationEngineCorpus corpus);
    Task<bool> DeleteCorpusAsync(string engineId, string corpusId);
}
