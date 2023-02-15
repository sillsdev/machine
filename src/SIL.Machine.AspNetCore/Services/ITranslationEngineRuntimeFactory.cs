namespace SIL.Machine.AspNetCore.Services;

public interface ITranslationEngineRuntimeFactory
{
    TranslationEngineType Type { get; }
    ITranslationEngineRuntime CreateTranslationEngineRuntime(string engineId);
}
