namespace SIL.Machine.WebApi.Services;

public interface IEngineService
{
	Task<TranslationResult> TranslateAsync(string engineId, IReadOnlyList<string> segment);

	Task<IEnumerable<TranslationResult>> TranslateAsync(string engineId, int n, IReadOnlyList<string> segment);

	Task<WordGraph> GetWordGraphAsync(string engineId, IReadOnlyList<string> segment);

	Task<bool> TrainSegmentAsync(string engineId, IReadOnlyList<string> sourceSegment,
		IReadOnlyList<string> targetSegment, bool sentenceStart);

	Task<bool> AddAsync(Engine engine);

	Task<bool> RemoveAsync(string engineId);

	Task<Build> StartBuildAsync(string engineId);

	Task CancelBuildAsync(string engineId);
}
