namespace SIL.Machine.WebApi.Services;

public class EngineRuntimeFactory<T> : IEngineRuntimeFactory where T : IEngineRuntime
{
	private readonly IServiceProvider _serviceProvider;

	public EngineRuntimeFactory(IServiceProvider serviceProvider, EngineType type)
	{
		_serviceProvider = serviceProvider;
		Type = type;
	}

	public EngineType Type { get; }

	public IEngineRuntime CreateEngineRuntime(string engineId)
	{
		return ActivatorUtilities.CreateInstance<T>(_serviceProvider, engineId);
	}
}
