namespace SIL.Machine.WebApi.Services;

public class TransferTruecaserFactory : IComponentFactory<ITruecaser>
{
	public Task<ITruecaser?> CreateAsync(string engineId)
	{
		return Task.FromResult<ITruecaser?>(new TransferTruecaser());
	}

	public void InitNew(string engineId)
	{
	}

	public void Cleanup(string engineId)
	{
	}
}
