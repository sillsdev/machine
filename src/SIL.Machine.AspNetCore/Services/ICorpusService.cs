namespace SIL.Machine.AspNetCore.Services;

public interface ICorpusService
{
    IDictionary<CorpusType, ITextCorpus> CreateTextCorpus(IReadOnlyList<CorpusFile> files);
}
