namespace SIL.Machine.AspNetCore.Services;

public class ThotSmtModelFactory(IOptionsMonitor<ThotSmtModelOptions> options) : ISmtModelFactory
{
    private readonly IOptionsMonitor<ThotSmtModelOptions> _options = options;

    public Task<IInteractiveTranslationModel> CreateAsync(
        string engineDir,
        IRangeTokenizer<string, int, string> tokenizer,
        IDetokenizer<string, string> detokenizer,
        ITruecaser truecaser,
        CancellationToken cancellationToken = default
    )
    {
        string smtConfigFileName = Path.Combine(engineDir, "smt.cfg");
        IInteractiveTranslationModel model = new ThotSmtModel(ThotWordAlignmentModelType.Hmm, smtConfigFileName)
        {
            SourceTokenizer = tokenizer,
            TargetTokenizer = tokenizer,
            TargetDetokenizer = detokenizer,
            LowercaseSource = true,
            LowercaseTarget = true,
            Truecaser = truecaser
        };
        return Task.FromResult(model);
    }

    public Task<ITrainer> CreateTrainerAsync(
        string engineDir,
        IRangeTokenizer<string, int, string> tokenizer,
        IParallelTextCorpus corpus,
        CancellationToken cancellationToken = default
    )
    {
        string smtConfigFileName = Path.Combine(engineDir, "smt.cfg");
        ITrainer trainer = new ThotSmtModelTrainer(ThotWordAlignmentModelType.Hmm, corpus, smtConfigFileName)
        {
            SourceTokenizer = tokenizer,
            TargetTokenizer = tokenizer,
            LowercaseSource = true,
            LowercaseTarget = true
        };
        return Task.FromResult(trainer);
    }

    public Task InitNewAsync(string engineDir, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(engineDir))
            Directory.CreateDirectory(engineDir);
        ZipFile.ExtractToDirectory(_options.CurrentValue.NewModelFile, engineDir);
        return Task.CompletedTask;
    }

    public Task CleanupAsync(string engineDir, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(engineDir))
            return Task.CompletedTask;
        DirectoryHelper.DeleteDirectoryRobust(Path.Combine(engineDir, "lm"));
        DirectoryHelper.DeleteDirectoryRobust(Path.Combine(engineDir, "tm"));
        string smtConfigFileName = Path.Combine(engineDir, "smt.cfg");
        if (File.Exists(smtConfigFileName))
            File.Delete(smtConfigFileName);
        if (!Directory.EnumerateFileSystemEntries(engineDir).Any())
            Directory.Delete(engineDir);
        return Task.CompletedTask;
    }

    public async Task UpdateEngineFromAsync(
        string engineDir,
        Stream source,
        CancellationToken cancellationToken = default
    )
    {
        if (!Directory.Exists(engineDir))
            Directory.CreateDirectory(engineDir);

        await using MemoryStream memoryStream = new();
        await using (GZipStream gzipStream = new(source, CompressionMode.Decompress))
        {
            await gzipStream.CopyToAsync(memoryStream, cancellationToken);
        }
        memoryStream.Seek(0, SeekOrigin.Begin);
        await TarFile.ExtractToDirectoryAsync(
            memoryStream,
            engineDir,
            overwriteFiles: true,
            cancellationToken: cancellationToken
        );
    }

    public async Task SaveEngineToAsync(
        string engineDir,
        Stream destination,
        CancellationToken cancellationToken = default
    )
    {
        // create zip archive in memory stream
        // This cannot be created directly to the shared stream because it all needs to be written at once
        await using MemoryStream memoryStream = new();
        await TarFile.CreateFromDirectoryAsync(
            engineDir,
            memoryStream,
            includeBaseDirectory: false,
            cancellationToken: cancellationToken
        );
        memoryStream.Seek(0, SeekOrigin.Begin);
        await using GZipStream gzipStream = new(destination, CompressionMode.Compress);
        await memoryStream.CopyToAsync(gzipStream, cancellationToken);
    }
}
