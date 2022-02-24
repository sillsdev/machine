namespace SIL.Machine.WebApi.Services;

public class EngineRuntimeFactory<T> : IEngineRuntimeFactory where T : IEngineRuntime
{
	private readonly IServiceProvider _serviceProvider;

	public EngineRuntimeFactory(IServiceProvider serviceProvider, string key)
	{
		_serviceProvider = serviceProvider;
		Key = key;
	}

	public string Key { get; }

	public IEngineRuntime CreateEngineRuntime(string engineId)
	{
		return ActivatorUtilities.CreateInstance<T>(_serviceProvider, engineId);
	}
}
