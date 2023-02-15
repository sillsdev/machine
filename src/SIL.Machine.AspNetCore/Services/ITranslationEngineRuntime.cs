namespace SIL.Machine.AspNetCore.Services;

public interface ITranslationEngineRuntime : IAsyncDisposable
{
    Task InitNewAsync();
    Task<IReadOnlyList<(string Translation, TranslationResult Result)>> TranslateAsync(int n, string segment);
    Task<WordGraph> GetWordGraphAsync(string segment);
    Task TrainSegmentPairAsync(string sourceSegment, string targetSegment, bool sentenceStart);
    Task StartBuildAsync(string buildId);
    Task CancelBuildAsync();
    Task CommitAsync();
    Task DeleteDataAsync();
}
