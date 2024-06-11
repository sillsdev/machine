namespace SIL.Machine.AspNetCore.Services;

public interface ITruecaserFactory
{
    Task<ITruecaser> CreateAsync(string engineDir, CancellationToken cancellationToken = default);
    Task<ITrainer> CreateTrainerAsync(
        string engineDir,
        ITokenizer<string, int, string> tokenizer,
        ITextCorpus corpus,
        CancellationToken cancellationToken = default
    );
    Task CleanupAsync(string engineDir, CancellationToken cancellationToken = default);
}
