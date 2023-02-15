namespace SIL.Machine.AspNetCore.Services;

public record TranslationEngineInfo(
    string Type,
    string Id,
    string Name,
    string SourceLanguageTag,
    string TargetLanguageTag
);

public record CorpusInfo(string Id, bool Pretranslate, ITextCorpus? SourceCorpus, ITextCorpus? TargetCorpus);

public record PretranslationInfo(string CorpusId, string TextId, List<string> Refs, string Translation);

public interface IPlatformService
{
    Task<TranslationEngineInfo> GetTranslationEngineInfoAsync(
        string engineId,
        CancellationToken cancellationToken = default
    );
    Task IncrementTrainSizeAsync(string engineId, int count = 1, CancellationToken cancellationToken = default);

    Task UpdateBuildStatusAsync(
        string buildId,
        ProgressStatus progressStatus,
        CancellationToken cancellationToken = default
    );
    Task UpdateBuildStatusAsync(string buildId, int step, CancellationToken cancellationToken = default);
    Task BuildStartedAsync(string buildId, CancellationToken cancellationToken = default);
    Task BuildCompletedAsync(
        string buildId,
        int trainSize,
        double confidence,
        CancellationToken cancellationToken = default
    );
    Task BuildCanceledAsync(string buildId, CancellationToken cancellationToken = default);
    Task BuildFaultedAsync(string buildId, string message, CancellationToken cancellationToken = default);
    Task BuildRestartingAsync(string buildId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<CorpusInfo> GetCorporaAsync(string engineId, CancellationToken cancellationToken = default);

    Task DeleteAllPretranslationsAsync(string engineId, CancellationToken cancellationToken = default);
    Task InsertPretranslationsAsync(
        string engineId,
        IAsyncEnumerable<PretranslationInfo> pretranslations,
        CancellationToken cancellationToken = default
    );
}
