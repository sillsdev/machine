namespace SIL.Machine.WebApi.Services;

public class DataFileService : IDataFileService
{
	private readonly IOptions<DataFileOptions> _dataFileOptions;
	private readonly IRepository<DataFile> _dataFiles;

	public DataFileService(IOptions<DataFileOptions> dataFileOptions, IRepository<DataFile> dataFiles)
	{
		_dataFileOptions = dataFileOptions;
		_dataFiles = dataFiles;
	}

	public async Task<DataFile> CreateAsync(string engineId, string name, string format, string dataType, Stream stream)
	{
		var dataFile = new DataFile
		{
			EngineRef = engineId,
			Name = name,
			Format = format,
			DataType = dataType,
			Filename = Path.GetRandomFileName()
		};
		string path = Path.Combine(_dataFileOptions.Value.DataFilesDir, dataFile.Filename);
		using (FileStream fileStream = File.Create(path))
		{
			await stream.CopyToAsync(fileStream);
		}

		await _dataFiles.InsertAsync(dataFile);
		return dataFile;
	}

	public async Task<bool> DeleteAsync(string id)
	{
		DataFile dataFile = await _dataFiles.DeleteAsync(id);
		if (dataFile == null)
			return false;

		string path = Path.Combine(_dataFileOptions.Value.DataFilesDir, dataFile.Filename);
		File.Delete(path);
		return true;
	}

	public async Task DeleteAllByEngineIdAsync(string engineId)
	{
		IReadOnlyList<DataFile> dataFiles = await _dataFiles.GetAllAsync(f => f.EngineRef == engineId);
		List<string> idsToDelete = dataFiles.Select(f => f.Id).ToList();
		await _dataFiles.DeleteAllAsync(f => idsToDelete.Contains(f.Id));
		foreach (DataFile dataFile in dataFiles)
		{
			string path = Path.Combine(_dataFileOptions.Value.DataFilesDir, dataFile.Filename);
			File.Delete(path);
		}
	}
}
