namespace SIL.Machine.WebApi.Services;

public interface ITranslationEngineRuntimeFactory
{
    TranslationEngineType Type { get; }
    ITranslationEngineRuntime CreateTranslationEngineRuntime(string engineId);
}
