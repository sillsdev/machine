namespace SIL.Machine.WebApi.Services;

public interface ITranslationEngineRuntimeService
{
    void Init();

    Task CreateAsync(TranslationEngine engine);
    Task DeleteAsync(TranslationEngine engine);

    Task<TranslationResult?> TranslateAsync(TranslationEngine engine, IReadOnlyList<string> segment);

    Task<IEnumerable<TranslationResult>?> TranslateAsync(
        TranslationEngine engine,
        int n,
        IReadOnlyList<string> segment
    );

    Task<WordGraph?> GetWordGraphAsync(TranslationEngine engine, IReadOnlyList<string> segment);

    Task TrainSegmentPairAsync(
        TranslationEngine engine,
        IReadOnlyList<string> sourceSegment,
        IReadOnlyList<string> targetSegment,
        bool sentenceStart
    );

    Task<Build?> StartBuildAsync(TranslationEngine engine);

    Task CancelBuildAsync(TranslationEngine engine);
}
