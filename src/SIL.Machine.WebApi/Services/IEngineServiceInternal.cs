namespace SIL.Machine.WebApi.Services;

internal interface IEngineServiceInternal : IEngineService
{
	void Init();
	Task<(Engine Engine, EngineRuntime Runtime)> GetEngineAsync(string engineId);
}
