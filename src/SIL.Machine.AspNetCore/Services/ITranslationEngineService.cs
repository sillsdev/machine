namespace SIL.Machine.AspNetCore.Services;

public interface ITranslationEngineService
{
    TranslationEngineType Type { get; }

    Task CreateAsync(
        string engineId,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default
    );
    Task DeleteAsync(string engineId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(string Translation, TranslationResult Result)>> TranslateAsync(
        string engineId,
        int n,
        string segment,
        CancellationToken cancellationToken = default
    );

    Task<WordGraph> GetWordGraphAsync(string engineId, string segment, CancellationToken cancellationToken = default);

    Task TrainSegmentPairAsync(
        string engineId,
        string sourceSegment,
        string targetSegment,
        bool sentenceStart,
        CancellationToken cancellationToken = default
    );

    Task StartBuildAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> corpora,
        CancellationToken cancellationToken = default
    );

    Task CancelBuildAsync(string engineId, CancellationToken cancellationToken = default);
}
