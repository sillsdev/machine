namespace SIL.Machine.WebApi.Services;

public interface ITranslationEngineRuntimeService
{
    void Init();

    Task CreateAsync(TranslationEngineType engineType, string engineId);
    Task DeleteAsync(TranslationEngineType engineType, string engineId);

    Task<TranslationResult> TranslateAsync(
        TranslationEngineType engineType,
        string engineId,
        IReadOnlyList<string> segment
    );

    Task<IEnumerable<TranslationResult>> TranslateAsync(
        TranslationEngineType engineType,
        string engineId,
        int n,
        IReadOnlyList<string> segment
    );

    Task<WordGraph> GetWordGraphAsync(TranslationEngineType engineType, string engineId, IReadOnlyList<string> segment);

    Task TrainSegmentPairAsync(
        TranslationEngineType engineType,
        string engineId,
        IReadOnlyList<string> sourceSegment,
        IReadOnlyList<string> targetSegment,
        bool sentenceStart
    );

    Task<Build> StartBuildAsync(TranslationEngineType engineType, string engineId);

    Task CancelBuildAsync(string engineId);
}
