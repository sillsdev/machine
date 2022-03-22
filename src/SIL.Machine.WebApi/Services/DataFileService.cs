namespace SIL.Machine.WebApi.Services;

public class DataFileService : IDataFileService
{
	private readonly IOptions<EngineOptions> _engineOptions;
	private readonly IRepository<DataFile> _dataFiles;

	public DataFileService(IOptions<EngineOptions> engineOptions, IRepository<DataFile> dataFiles)
	{
		_engineOptions = engineOptions;
		_dataFiles = dataFiles;
	}

	public async Task CreateAsync(DataFile dataFile, Stream stream)
	{
		dataFile.Filename = Path.GetRandomFileName();
		string path = GetDataFilePath(dataFile);
		using (FileStream fileStream = File.Create(path))
		{
			await stream.CopyToAsync(fileStream);
		}

		await _dataFiles.InsertAsync(dataFile);
	}

	public async Task<bool> DeleteAsync(string id)
	{
		DataFile? dataFile = await _dataFiles.DeleteAsync(id);
		if (dataFile == null)
			return false;

		string path = GetDataFilePath(dataFile);
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
			string path = Path.Combine(_engineOptions.Value.DataFilesDir, dataFile.Filename);
			File.Delete(path);
		}
	}

	public async Task<IReadOnlyDictionary<string, ITextCorpus>> CreateTextCorporaAsync(string engineId,
		CorpusType corpusType)
	{
		IReadOnlyList<DataFile> dataFiles = await _dataFiles.GetAllAsync(
			f => f.EngineRef == engineId && f.DataType == DataType.TextCorpus && f.CorpusType == corpusType);

		var corpora = new Dictionary<string, ITextCorpus>();
		foreach (IGrouping<string?, DataFile> corpusGrouping in dataFiles.GroupBy(f => f.CorpusKey))
		{
			if (corpusGrouping.Key is null)
				continue;

			List<DataFile> corpusDataFiles = corpusGrouping.ToList();
			if (corpusDataFiles.Count == 0)
				continue;
			FileFormat format = corpusDataFiles[0].Format;
			ITextCorpus? corpus = null;
			switch (format)
			{
				case FileFormat.Text:
					corpus = new DictionaryTextCorpus(corpusDataFiles
						.Select(f => new TextFileText(f.Name, GetDataFilePath(f))));
					break;

				case FileFormat.Paratext:
					corpus = new ParatextBackupTextCorpus(GetDataFilePath(corpusDataFiles[0]));
					break;
			}

			if (corpus is not null)
				corpora[corpusGrouping.Key] = corpus;
		}

		return corpora;
	}

	private string GetDataFilePath(DataFile dataFile)
	{
		return Path.Combine(_engineOptions.Value.DataFilesDir, dataFile.Filename);
	}
}
