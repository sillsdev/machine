namespace SIL.Machine.AspNetCore.Services;

public interface ITranslationEngineService
{
    TranslationEngineType Type { get; }

    Task<TranslationEngine> CreateAsync(
        string engineId,
        string? engineName,
        string sourceLanguage,
        string targetLanguage,
        bool? isModelPersisted = null,
        CancellationToken cancellationToken = default
    );
    Task DeleteAsync(string engineId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TranslationResult>> TranslateAsync(
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
        string? buildOptions,
        IReadOnlyList<Corpus> corpora,
        CancellationToken cancellationToken = default
    );

    Task CancelBuildAsync(string engineId, CancellationToken cancellationToken = default);

    Task<ModelDownloadUrl> GetModelDownloadUrlAsync(string engineId, CancellationToken cancellationToken = default);

    int GetQueueSize();

    bool IsLanguageNativeToModel(string language, out string internalCode);
}
