namespace SIL.Machine.AspNetCore.Services;

public interface ITransferEngineFactory
{
    ITranslationEngine? Create(
        string engineId,
        IRangeTokenizer<string, int, string> tokenizer,
        IDetokenizer<string, string> detokenizer,
        ITruecaser truecaser
    );
    void InitNew(string engineId);
    void Cleanup(string engineId);
}
