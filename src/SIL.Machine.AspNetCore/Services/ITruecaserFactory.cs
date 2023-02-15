namespace SIL.Machine.AspNetCore.Services;

public interface ITruecaserFactory
{
    Task<ITruecaser> CreateAsync(string engineId);
    ITrainer CreateTrainer(string engineId, ITextCorpus corpus);
    void Cleanup(string engineId);
}
