namespace SIL.Machine.WebApi.Services;

public interface ITruecaserFactory
{
	Task<ITruecaser> CreateAsync(string engineId);
	ITrainer CreateTrainer(string engineId, IEnumerable<TextRow> corpus);
	void Cleanup(string engineId);
}
