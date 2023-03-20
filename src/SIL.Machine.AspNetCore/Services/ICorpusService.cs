namespace SIL.Machine.AspNetCore.Services;

public interface ICorpusService
{
    ITextCorpus CreateTextCorpus(IReadOnlyList<CorpusFile> files);
}
