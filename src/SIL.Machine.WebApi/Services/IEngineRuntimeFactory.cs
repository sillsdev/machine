namespace SIL.Machine.WebApi.Services;

public interface IEngineRuntimeFactory
{
    EngineType Type { get; }
    IEngineRuntime CreateEngineRuntime(string engineId);
}
