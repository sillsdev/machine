namespace SIL.Machine.WebApi.Services;

public interface IDataFileService
{
	Task CreateAsync(DataFile dataFile, Stream stream);
	Task<bool> DeleteAsync(string id);
	Task DeleteAllByEngineIdAsync(string engineId);
	Task<IReadOnlyDictionary<string, ITextCorpus>> CreateTextCorporaAsync(string engineId, CorpusType corpusType);
}
