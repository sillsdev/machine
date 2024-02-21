﻿namespace SIL.Machine.AspNetCore.Services;

public class TransferEngineFactory(IOptionsMonitor<SmtTransferEngineOptions> engineOptions) : ITransferEngineFactory
{
    private readonly IOptionsMonitor<SmtTransferEngineOptions> _engineOptions = engineOptions;

    public ITranslationEngine? Create(
        string engineId,
        IRangeTokenizer<string, int, string> tokenizer,
        IDetokenizer<string, string> detokenizer,
        ITruecaser truecaser
    )
    {
        string engineDir = Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId);
        string hcSrcConfigFileName = Path.Combine(engineDir, "src-hc.xml");
        string hcTrgConfigFileName = Path.Combine(engineDir, "trg-hc.xml");
        TransferEngine? transferEngine = null;
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
        return transferEngine;
    }

    public void InitNew(string engineId)
    {
        // TODO: generate source and target config files
    }

    public void Cleanup(string engineId)
    {
        string engineDir = Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId);
        if (!Directory.Exists(engineDir))
            return;
        string hcSrcConfigFileName = Path.Combine(engineDir, "src-hc.xml");
        if (File.Exists(hcSrcConfigFileName))
            File.Delete(hcSrcConfigFileName);
        string hcTrgConfigFileName = Path.Combine(engineDir, "trg-hc.xml");
        if (File.Exists(hcTrgConfigFileName))
            File.Delete(hcTrgConfigFileName);
        if (!Directory.EnumerateFileSystemEntries(engineDir).Any())
            Directory.Delete(engineDir);
    }
}
