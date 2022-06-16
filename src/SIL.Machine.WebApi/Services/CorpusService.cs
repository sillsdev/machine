namespace SIL.Machine.WebApi.Services;

public class CorpusService : EntityServiceBase<Corpus>, ICorpusService
{
    private readonly IOptionsMonitor<CorpusOptions> _options;

    public CorpusService(IRepository<Corpus> corpora, IOptionsMonitor<CorpusOptions> options) : base(corpora)
    {
        _options = options;
    }

    public async Task<IEnumerable<Corpus>> GetAllAsync(string owner)
    {
        CheckDisposed();

        return await Entities.GetAllAsync(c => c.Owner == owner);
    }

    public async Task AddDataFileAsync(string id, DataFile dataFile, Stream stream)
    {
        CheckDisposed();

        dataFile.Filename = Path.GetRandomFileName();
        string path = GetDataFilePath(dataFile);
        using (FileStream fileStream = File.Create(path))
        {
            await stream.CopyToAsync(fileStream);
        }
        await Entities.UpdateAsync(id, u => u.Add(c => c.Files, dataFile));
    }

    public async Task<bool> DeleteDataFileAsync(string id, string fileId)
    {
        CheckDisposed();

        Corpus? corpus = await GetAsync(id);
        if (corpus == null)
            return false;
        DataFile? file = corpus.Files.FirstOrDefault(f => f.Id == fileId);
        if (file == null)
            return false;
        await Entities.UpdateAsync(id, u => u.RemoveAll(c => c.Files, f => f.Id == fileId));
        string path = GetDataFilePath(file);
        if (File.Exists(path))
            File.Delete(path);
        return true;
    }

    public async Task<ITextCorpus?> CreateTextCorpusAsync(string id, string languageTag)
    {
        CheckDisposed();

        Corpus? corpus = await GetAsync(id);
        if (corpus is null || corpus.Type != CorpusType.Text)
            return null;
        DataFile[] files = corpus.Files.Where(f => f.LanguageTag == languageTag).ToArray();
        if (files.Length == 0)
            return null;

        ITextCorpus? textCorpus = null;
        switch (corpus.Format)
        {
            case FileFormat.Text:
                textCorpus = new DictionaryTextCorpus(
                    files.Select(f => new TextFileText(f.TextId ?? f.Name, GetDataFilePath(f)))
                );
                break;

            case FileFormat.Paratext:
                textCorpus = new ParatextBackupTextCorpus(GetDataFilePath(files[0]));
                break;
        }
        return textCorpus;
    }

    private string GetDataFilePath(DataFile dataFile)
    {
        return Path.Combine(_options.CurrentValue.DataFilesDir, dataFile.Filename);
    }
}
