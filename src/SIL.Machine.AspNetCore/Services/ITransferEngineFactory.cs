namespace SIL.Machine.AspNetCore.Services;

public interface ITransferEngineFactory
{
    ITranslationEngine? Create(string engineId);
    void InitNew(string engineId);
    void Cleanup(string engineId);
}
