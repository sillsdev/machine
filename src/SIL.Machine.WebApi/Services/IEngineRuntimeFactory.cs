namespace SIL.Machine.WebApi.Services;

public interface IEngineRuntimeFactory
{
	string Key { get; }
	IEngineRuntime CreateEngineRuntime(string engineId);
}
