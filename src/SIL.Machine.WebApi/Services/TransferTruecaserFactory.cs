namespace SIL.Machine.WebApi.Services;

public class TransferTruecaserFactory : ITruecaserFactory
{
	public Task<ITruecaser> CreateAsync(string engineId)
	{
		return Task.FromResult<ITruecaser>(new TransferTruecaser());
	}

	public ITrainer CreateTrainer(string engineId, IEnumerable<TextRow> corpus)
	{
		return new NullTrainer();
	}

	public void Cleanup(string engineId)
	{
	}
}
