namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferTrainBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    IBuildJobService buildJobService,
    ILogger<SmtTransferTrainBuildJob> logger,
    ISharedFileService sharedFileService,
    ITruecaserFactory truecaserFactory,
    ISmtModelFactory smtModelFactory
) : HangfireBuildJob<object?>(platformService, engines, lockFactory, buildJobService, logger)
{
    private readonly ISharedFileService _sharedFileService = sharedFileService;
    private readonly ITruecaserFactory _truecaserFactory = truecaserFactory;
    private readonly ISmtModelFactory _smtModelFactory = smtModelFactory;

    protected override async Task DoWorkAsync(
        string engineId,
        string buildId,
        object? data,
        string? buildOptions,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();
        await DownloadTrainingText(buildId, tempDir.FullName, cancellationToken);

        // assemble corpus
        DictionaryTextCorpus sourceCorpus =
            new(new TextFileText("train", Path.Combine(tempDir.FullName, "train.src.txt")));
        DictionaryTextCorpus targetCorpus =
            new(new TextFileText("train", Path.Combine(tempDir.FullName, "train.trg.txt")));
        ParallelTextCorpus parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
        int corpusSize = parallelCorpus.Count(includeEmpty: false);

        // train SMT model
        var tokenizer = new LatinWordTokenizer();

        using ITrainer smtModelTrainer = _smtModelFactory.CreateTrainer(engineId, tokenizer, parallelCorpus);
        using ITrainer truecaseTrainer = _truecaserFactory.CreateTrainer(engineId, tokenizer, targetCorpus);

        cancellationToken.ThrowIfCancellationRequested();

        var progress = new BuildProgress(PlatformService, buildId);
        await smtModelTrainer.TrainAsync(progress, cancellationToken);
        await truecaseTrainer.TrainAsync(cancellationToken: cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        await smtModelTrainer.SaveAsync(CancellationToken.None);
        await truecaseTrainer.SaveAsync(CancellationToken.None);

        await _smtModelFactory.UploadBuiltEngineAsync(engineId, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            bool canceling = !await BuildJobService.StartBuildJobAsync(
                BuildJobRunnerType.Hangfire,
                engineId,
                buildId,
                BuildStage.Postprocess,
                data: (smtModelTrainer.Stats.TrainCorpusSize, smtModelTrainer.Stats.Metrics["bleu"] * 100.0),
                buildOptions: buildOptions,
                cancellationToken: cancellationToken
            );
            if (canceling)
                throw new OperationCanceledException();
        }
    }

    private async Task DownloadTrainingText(string buildId, string directory, CancellationToken cancellationToken)
    {
        using Stream srcText = await _sharedFileService.OpenReadAsync(
            $"builds/{buildId}/train.src.txt",
            cancellationToken
        );
        using FileStream srcFileStream = File.Create(Path.Combine(directory, "train.src.txt"));
        await srcText.CopyToAsync(srcFileStream, cancellationToken);

        using Stream tgtText = await _sharedFileService.OpenReadAsync(
            $"builds/{buildId}/train.trg.txt",
            cancellationToken
        );
        using FileStream tgtFileStream = File.Create(Path.Combine(directory, "train.trg.txt"));
        await tgtText.CopyToAsync(tgtFileStream, cancellationToken);
    }
}
