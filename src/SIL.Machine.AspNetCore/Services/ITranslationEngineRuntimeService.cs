namespace SIL.Machine.AspNetCore.Services;

public interface ITranslationEngineRuntimeService
{
    void Init();

    Task CreateAsync(TranslationEngineType engineType, string engineId);
    Task DeleteAsync(TranslationEngineType engineType, string engineId);

    Task<IEnumerable<(string Translation, TranslationResult Result)>> TranslateAsync(
        TranslationEngineType engineType,
        string engineId,
        int n,
        string segment
    );

    Task<WordGraph> GetWordGraphAsync(TranslationEngineType engineType, string engineId, string segment);

    Task TrainSegmentPairAsync(
        TranslationEngineType engineType,
        string engineId,
        string sourceSegment,
        string targetSegment,
        bool sentenceStart
    );

    Task StartBuildAsync(TranslationEngineType engineType, string engineId, string buildId);

    Task CancelBuildAsync(string engineId);
}
