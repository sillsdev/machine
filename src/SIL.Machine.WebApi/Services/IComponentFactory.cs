namespace SIL.Machine.WebApi.Services;

public interface IComponentFactory<T>
{
	Task<T> CreateAsync(string engineId);
	void InitNew(string engineId);
	void Cleanup(string engineId);
}
