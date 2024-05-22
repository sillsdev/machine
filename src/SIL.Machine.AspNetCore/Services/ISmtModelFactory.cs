﻿namespace SIL.Machine.AspNetCore.Services;

public interface ISmtModelFactory
{
    IInteractiveTranslationModel Create(
        string engineId,
        IRangeTokenizer<string, int, string> tokenizer,
        IDetokenizer<string, string> detokenizer,
        ITruecaser truecaser
    );
    ITrainer CreateTrainer(string engineId, IRangeTokenizer<string, int, string> tokenizer, IParallelTextCorpus corpus);
    Task DownloadBuiltEngineAsync(string engineId, CancellationToken cancellationToken);
    void InitNew(string engineId);
    void Cleanup(string engineId);
}
