namespace SIL.Machine.WebApi.Services;

public interface ITruecaserFactory
{
	Task<ITruecaser> CreateAsync(string engineId);
	void Cleanup(string engineId);
}
