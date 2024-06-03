namespace SIL.Machine.AspNetCore.Services;

public class UnigramTruecaserFactory : ITruecaserFactory
{
    public async Task<ITruecaser> CreateAsync(string engineDir, CancellationToken cancellationToken = default)
    {
        var truecaser = new UnigramTruecaser();
        string path = GetModelPath(engineDir);
        await truecaser.LoadAsync(path);
        return truecaser;
    }

    public Task<ITrainer> CreateTrainerAsync(
        string engineDir,
        ITokenizer<string, int, string> tokenizer,
        ITextCorpus corpus,
        CancellationToken cancellationToken = default
    )
    {
        string path = GetModelPath(engineDir);
        ITrainer trainer = new UnigramTruecaserTrainer(path, corpus) { Tokenizer = tokenizer };
        return Task.FromResult(trainer);
    }

    public Task CleanupAsync(string engineDir, CancellationToken cancellationToken = default)
    {
        string path = GetModelPath(engineDir);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private static string GetModelPath(string engineDir)
    {
        return Path.Combine(engineDir, "unigram-casing-model.txt");
    }
}
