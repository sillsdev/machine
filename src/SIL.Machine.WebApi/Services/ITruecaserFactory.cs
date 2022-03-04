namespace SIL.Machine.WebApi.Services;

public interface ITruecaserFactory
{
	Task<ITruecaser> CreateAsync(string engineId);
	ITrainer CreateTrainer(string engineId, ITextCorpus textCorpus);
	void Cleanup(string engineId);
}
