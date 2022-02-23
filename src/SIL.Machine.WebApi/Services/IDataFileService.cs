namespace SIL.Machine.WebApi.Services;

public interface IDataFileService
{
	Task<DataFile> CreateAsync(string engineId, string name, string format, string dataType, Stream stream);
	Task<bool> DeleteAsync(string id);
	Task DeleteAllByEngineIdAsync(string engineId);
}
