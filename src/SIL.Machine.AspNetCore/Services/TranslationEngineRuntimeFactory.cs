namespace SIL.Machine.AspNetCore.Services;

public class TranslationEngineRuntimeFactory<T> : ITranslationEngineRuntimeFactory where T : ITranslationEngineRuntime
{
    private readonly IServiceProvider _serviceProvider;

    public TranslationEngineRuntimeFactory(IServiceProvider serviceProvider, TranslationEngineType type)
    {
        _serviceProvider = serviceProvider;
        Type = type;
    }

    public TranslationEngineType Type { get; }

    public ITranslationEngineRuntime CreateTranslationEngineRuntime(string engineId)
    {
        return ActivatorUtilities.CreateInstance<T>(_serviceProvider, engineId);
    }
}
