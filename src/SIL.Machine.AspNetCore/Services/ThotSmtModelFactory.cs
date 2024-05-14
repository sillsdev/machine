namespace SIL.Machine.AspNetCore.Services;

public class ThotSmtModelFactory(
    IOptionsMonitor<ThotSmtModelOptions> options,
    IOptionsMonitor<SmtTransferEngineOptions> engineOptions,
    ISharedFileService sharedFileService
) : ISmtModelFactory
{
    private readonly IOptionsMonitor<ThotSmtModelOptions> _options = options;
    private readonly IOptionsMonitor<SmtTransferEngineOptions> _engineOptions = engineOptions;
    private readonly ISharedFileService _sharedFileService = sharedFileService;

    public IInteractiveTranslationModel Create(
        string engineId,
        IRangeTokenizer<string, int, string> tokenizer,
        IDetokenizer<string, string> detokenizer,
        ITruecaser truecaser
    )
    {
        string smtConfigFileName = Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId, "smt.cfg");
        var model = new ThotSmtModel(ThotWordAlignmentModelType.Hmm, smtConfigFileName)
        {
            SourceTokenizer = tokenizer,
            TargetTokenizer = tokenizer,
            TargetDetokenizer = detokenizer,
            LowercaseSource = true,
            LowercaseTarget = true,
            Truecaser = truecaser
        };
        return model;
    }

    public ITrainer CreateTrainer(
        string engineId,
        IRangeTokenizer<string, int, string> tokenizer,
        IParallelTextCorpus corpus
    )
    {
        string smtConfigFileName = Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId, "smt.cfg");
        return new ThotSmtModelTrainer(ThotWordAlignmentModelType.Hmm, corpus, smtConfigFileName)
        {
            SourceTokenizer = tokenizer,
            TargetTokenizer = tokenizer,
            LowercaseSource = true,
            LowercaseTarget = true
        };
    }

    public async Task DownloadBuiltEngineAsync(string engineId, CancellationToken cancellationToken)
    {
        string engineDir = Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId);
        if (!Directory.Exists(engineDir))
            Directory.CreateDirectory(engineDir);
        string sharedFilePath = $"models/{engineId}.zip";
        using Stream sharedStream = await _sharedFileService.OpenReadAsync(sharedFilePath, cancellationToken);
        ZipFile.ExtractToDirectory(sharedStream, engineDir, overwriteFiles: true);
        await _sharedFileService.DeleteAsync(sharedFilePath);
    }

    public async Task UploadBuiltEngineAsync(string engineId, CancellationToken cancellationToken)
    {
        string engineDir = Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId);
        string sharedFilePath = $"models/{engineId}.zip";
        using Stream sharedStream = await _sharedFileService.OpenWriteAsync(sharedFilePath, cancellationToken);
        ZipFile.CreateFromDirectory(engineDir, sharedStream);
    }

    public void InitNew(string engineId)
    {
        string engineDir = Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId);
        if (!Directory.Exists(engineDir))
            Directory.CreateDirectory(engineDir);
        ZipFile.ExtractToDirectory(_options.CurrentValue.NewModelFile, engineDir);
    }

    public void Cleanup(string engineId)
    {
        string engineDir = Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId);
        if (!Directory.Exists(engineDir))
            return;
        DirectoryHelper.DeleteDirectoryRobust(Path.Combine(engineDir, "lm"));
        DirectoryHelper.DeleteDirectoryRobust(Path.Combine(engineDir, "tm"));
        string smtConfigFileName = Path.Combine(engineDir, "smt.cfg");
        if (File.Exists(smtConfigFileName))
            File.Delete(smtConfigFileName);
        if (!Directory.EnumerateFileSystemEntries(engineDir).Any())
            Directory.Delete(engineDir);
    }
}
