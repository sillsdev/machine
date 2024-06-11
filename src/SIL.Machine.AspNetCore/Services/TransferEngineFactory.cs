namespace SIL.Machine.AspNetCore.Services;

public class TransferEngineFactory : ITransferEngineFactory
{
    public Task<ITranslationEngine?> CreateAsync(
        string engineDir,
        IRangeTokenizer<string, int, string> tokenizer,
        IDetokenizer<string, string> detokenizer,
        ITruecaser truecaser,
        CancellationToken cancellationToken = default
    )
    {
        string hcSrcConfigFileName = Path.Combine(engineDir, "src-hc.xml");
        string hcTrgConfigFileName = Path.Combine(engineDir, "trg-hc.xml");
        ITranslationEngine? transferEngine = null;
        if (File.Exists(hcSrcConfigFileName) && File.Exists(hcTrgConfigFileName))
        {
            var hcTraceManager = new TraceManager();

            Language srcLang = XmlLanguageLoader.Load(hcSrcConfigFileName);
            var srcMorpher = new Morpher(hcTraceManager, srcLang);

            Language trgLang = XmlLanguageLoader.Load(hcTrgConfigFileName);
            var trgMorpher = new Morpher(hcTraceManager, trgLang);

            transferEngine = new TransferEngine(
                srcMorpher,
                new SimpleTransferer(new GlossMorphemeMapper(trgMorpher)),
                trgMorpher
            )
            {
                SourceTokenizer = tokenizer,
                TargetDetokenizer = detokenizer,
                LowercaseSource = true,
                Truecaser = truecaser
            };
        }
        return Task.FromResult(transferEngine);
    }

    public Task InitNewAsync(string engineDir, CancellationToken cancellationToken = default)
    {
        // TODO: generate source and target config files
        return Task.CompletedTask;
    }

    public Task CleanupAsync(string engineDir, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(engineDir))
            return Task.CompletedTask;
        string hcSrcConfigFileName = Path.Combine(engineDir, "src-hc.xml");
        if (File.Exists(hcSrcConfigFileName))
            File.Delete(hcSrcConfigFileName);
        string hcTrgConfigFileName = Path.Combine(engineDir, "trg-hc.xml");
        if (File.Exists(hcTrgConfigFileName))
            File.Delete(hcTrgConfigFileName);
        if (!Directory.EnumerateFileSystemEntries(engineDir).Any())
            Directory.Delete(engineDir);
        return Task.CompletedTask;
    }
}
