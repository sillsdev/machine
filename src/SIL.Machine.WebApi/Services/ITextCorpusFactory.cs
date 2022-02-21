namespace SIL.Machine.WebApi.Services;

public enum TextCorpusType
{
	Source,
	Target
}

public interface ITextCorpusFactory
{
	Task<ITextCorpus> CreateAsync(string engineId, TextCorpusType type);
}
