namespace SIL.Machine.WebApi.Services;

public interface IEngineService
{
	void Init();

	Task<TranslationResult?> TranslateAsync(string engineId, IReadOnlyList<string> segment);

	Task<IEnumerable<TranslationResult>?> TranslateAsync(string engineId, int n, IReadOnlyList<string> segment);

	Task<WordGraph?> GetWordGraphAsync(string engineId, IReadOnlyList<string> segment);

	Task<bool> TrainSegmentAsync(string engineId, IReadOnlyList<string> sourceSegment,
		IReadOnlyList<string> targetSegment, bool sentenceStart);

	Task CreateAsync(Engine engine);

	Task<bool> DeleteAsync(string engineId);

	Task<Build?> StartBuildAsync(string engineId);

	Task CancelBuildAsync(string engineId);
}
