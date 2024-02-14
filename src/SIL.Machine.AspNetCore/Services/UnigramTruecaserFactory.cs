namespace SIL.Machine.AspNetCore.Services;

public class UnigramTruecaserFactory(IOptionsMonitor<SmtTransferEngineOptions> engineOptions) : ITruecaserFactory
{
    private readonly IOptionsMonitor<SmtTransferEngineOptions> _engineOptions = engineOptions;

    public async Task<ITruecaser> CreateAsync(string engineId)
    {
        var truecaser = new UnigramTruecaser();
        string path = GetModelPath(engineId);
        await truecaser.LoadAsync(path);
        return truecaser;
    }

    public ITrainer CreateTrainer(string engineId, ITokenizer<string, int, string> tokenizer, ITextCorpus corpus)
    {
        string path = GetModelPath(engineId);
        return new UnigramTruecaserTrainer(path, corpus) { Tokenizer = tokenizer };
    }

    public void Cleanup(string engineId)
    {
        string path = GetModelPath(engineId);
        if (File.Exists(path))
            File.Delete(path);
    }

    private string GetModelPath(string engineId)
    {
        return Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId, "unigram-casing-model.txt");
    }
}
