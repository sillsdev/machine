namespace SIL.Machine.WebApi.Services;

public interface IEngineRuntime : IAsyncDisposable
{
	Task InitNewAsync();
	Task<TranslationResult> TranslateAsync(IReadOnlyList<string> segment);
	Task<IReadOnlyList<TranslationResult>> TranslateAsync(int n, IReadOnlyList<string> segment);
	Task<WordGraph> GetWordGraphAsync(IReadOnlyList<string> segment);
	Task TrainSegmentPairAsync(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment,
		bool sentenceStart);
	Task<Build> StartBuildAsync();
	Task CancelBuildAsync();
	Task CommitAsync();
	Task DeleteDataAsync();
}
