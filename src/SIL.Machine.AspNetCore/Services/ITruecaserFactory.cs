namespace SIL.Machine.AspNetCore.Services;

public interface ITruecaserFactory
{
    Task<ITruecaser> CreateAsync(string engineId);
    ITrainer CreateTrainer(string engineId, ITokenizer<string, int, string> tokenizer, ITextCorpus corpus);
    void Cleanup(string engineId);
}
