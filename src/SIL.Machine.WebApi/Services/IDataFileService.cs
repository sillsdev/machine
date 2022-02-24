namespace SIL.Machine.WebApi.Services;

public interface IDataFileService
{
	Task CreateAsync(DataFile dataFile, Stream stream);
	Task<bool> DeleteAsync(string id);
	Task DeleteAllByEngineIdAsync(string engineId);
	Task<ITextCorpus> CreateTextCorpusAsync(string engineId, CorpusType corpusType,
		ITokenizer<string, int, string> tokenizer);
}
