namespace SIL.Machine.WebApi.Services;

public interface ICorpusService
{
	Task<IEnumerable<Corpus>> GetAllAsync(string owner);
	Task<Corpus?> GetAsync(string id, CancellationToken cancellationToken = default);

	Task CreateAsync(Corpus corpus);
	Task<bool> DeleteAsync(string id);

	Task AddDataFileAsync(string corpusId, DataFile dataFile, Stream stream);
	Task<bool> DeleteDataFileAsync(string corpusId, string fileId);

	Task<ITextCorpus?> CreateTextCorpusAsync(string id, string languageTag);
}
