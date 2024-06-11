namespace SIL.Machine.AspNetCore.Services;

public interface ISmtModelFactory
{
    Task<IInteractiveTranslationModel> CreateAsync(
        string engineDir,
        IRangeTokenizer<string, int, string> tokenizer,
        IDetokenizer<string, string> detokenizer,
        ITruecaser truecaser,
        CancellationToken cancellationToken = default
    );
    Task<ITrainer> CreateTrainerAsync(
        string engineDir,
        IRangeTokenizer<string, int, string> tokenizer,
        IParallelTextCorpus corpus,
        CancellationToken cancellationToken = default
    );
    Task InitNewAsync(string engineDir, CancellationToken cancellationToken = default);
    Task CleanupAsync(string engineDir, CancellationToken cancellationToken = default);
    Task UpdateEngineFromAsync(string engineDir, Stream source, CancellationToken cancellationToken = default);
    Task SaveEngineToAsync(string engineDir, Stream destination, CancellationToken cancellationToken = default);
}
