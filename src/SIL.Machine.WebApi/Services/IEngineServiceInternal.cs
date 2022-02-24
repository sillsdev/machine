namespace SIL.Machine.WebApi.Services;

internal interface IEngineServiceInternal : IEngineService
{
	void Init();
	Task<(Engine? Engine, IEngineRuntime? Runtime)> GetEngineAsync(string engineId);
}
