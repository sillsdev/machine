namespace SIL.Machine.WebApi.Services;

public interface ITranslationEngineService
{
	void Init();

	Task<IEnumerable<TranslationEngine>> GetAllAsync(string owner);
	Task<TranslationEngine?> GetAsync(string engineId, CancellationToken cancellationToken = default);

	Task CreateAsync(TranslationEngine engine);
	Task<bool> DeleteAsync(string engineId);

	Task<TranslationResult?> TranslateAsync(string engineId, IReadOnlyList<string> segment);

	Task<IEnumerable<TranslationResult>?> TranslateAsync(string engineId, int n, IReadOnlyList<string> segment);

	Task<WordGraph?> GetWordGraphAsync(string engineId, IReadOnlyList<string> segment);

	Task<bool> TrainSegmentAsync(string engineId, IReadOnlyList<string> sourceSegment,
		IReadOnlyList<string> targetSegment, bool sentenceStart);

	Task<Build?> StartBuildAsync(string engineId);

	Task CancelBuildAsync(string engineId);

	Task AddCorpusAsync(string engineId, TranslationEngineCorpus corpus);
	Task<bool> DeleteCorpusAsync(string engineId, string corpusId);
}
