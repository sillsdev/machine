namespace SIL.Machine.AspNetCore.Services;

public interface ITransferEngineFactory
{
    Task<ITranslationEngine?> CreateAsync(
        string engineDir,
        IRangeTokenizer<string, int, string> tokenizer,
        IDetokenizer<string, string> detokenizer,
        ITruecaser truecaser,
        CancellationToken cancellationToken = default
    );
    Task InitNewAsync(string engineDir, CancellationToken cancellationToken = default);
    Task CleanupAsync(string engineDir, CancellationToken cancellationToken = default);
}
