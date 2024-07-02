namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferTrainBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    IDataAccessContext dataAccessContext,
    IBuildJobService buildJobService,
    ILogger<SmtTransferTrainBuildJob> logger,
    ISharedFileService sharedFileService,
    ITruecaserFactory truecaserFactory,
    ISmtModelFactory smtModelFactory,
    ITransferEngineFactory transferEngineFactory
) : HangfireBuildJob(platformService, engines, lockFactory, dataAccessContext, buildJobService, logger)
{
    private static readonly JsonWriterOptions PretranslateWriterOptions = new() { Indented = true };
    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private const int BatchSize = 128;

    private readonly ISharedFileService _sharedFileService = sharedFileService;
    private readonly ITruecaserFactory _truecaserFactory = truecaserFactory;
    private readonly ISmtModelFactory _smtModelFactory = smtModelFactory;
    private readonly ITransferEngineFactory _transferEngineFactory = transferEngineFactory;

    protected override async Task DoWorkAsync(
        string engineId,
        string buildId,
        object? data,
        string? buildOptions,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        using TempDirectory tempDir = new(buildId);
        string corpusDir = Path.Combine(tempDir.Path, "corpus");
        await DownloadDataAsync(buildId, corpusDir, cancellationToken);

        // assemble corpus
        ITextCorpus sourceCorpus = new TextFileTextCorpus(Path.Combine(corpusDir, "train.src.txt"));
        ITextCorpus targetCorpus = new TextFileTextCorpus(Path.Combine(corpusDir, "train.trg.txt"));
        IParallelTextCorpus parallelCorpus = sourceCorpus.AlignRows(targetCorpus);

        // train SMT model
        string engineDir = Path.Combine(tempDir.Path, "engine");
        (int trainCorpusSize, double confidence) = await TrainAsync(
            buildId,
            engineDir,
            targetCorpus,
            parallelCorpus,
            cancellationToken
        );

        cancellationToken.ThrowIfCancellationRequested();

        await GeneratePretranslationsAsync(buildId, engineDir, cancellationToken);

        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            bool canceling = !await BuildJobService.StartBuildJobAsync(
                BuildJobRunnerType.Hangfire,
                engineId,
                buildId,
                BuildStage.Postprocess,
                data: (trainCorpusSize, confidence),
                buildOptions: buildOptions,
                cancellationToken: cancellationToken
            );
            if (canceling)
                throw new OperationCanceledException();
        }
    }

    protected override async Task CleanupAsync(
        string engineId,
        string buildId,
        object? data,
        IDistributedReaderWriterLock @lock,
        JobCompletionStatus completionStatus
    )
    {
        if (completionStatus is JobCompletionStatus.Canceled)
        {
            try
            {
                await _sharedFileService.DeleteAsync($"builds/{buildId}/");
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Unable to to delete job data for build {BuildId}.", buildId);
            }
        }
    }

    private async Task DownloadDataAsync(string buildId, string corpusDir, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(corpusDir);
        await using Stream srcText = await _sharedFileService.OpenReadAsync(
            $"builds/{buildId}/train.src.txt",
            cancellationToken
        );
        await using FileStream srcFileStream = File.Create(Path.Combine(corpusDir, "train.src.txt"));
        await srcText.CopyToAsync(srcFileStream, cancellationToken);

        await using Stream tgtText = await _sharedFileService.OpenReadAsync(
            $"builds/{buildId}/train.trg.txt",
            cancellationToken
        );
        await using FileStream tgtFileStream = File.Create(Path.Combine(corpusDir, "train.trg.txt"));
        await tgtText.CopyToAsync(tgtFileStream, cancellationToken);
    }

    private async Task<(int TrainCorpusSize, double Confidence)> TrainAsync(
        string buildId,
        string engineDir,
        ITextCorpus targetCorpus,
        IParallelTextCorpus parallelCorpus,
        CancellationToken cancellationToken
    )
    {
        await _smtModelFactory.InitNewAsync(engineDir, cancellationToken);
        LatinWordTokenizer tokenizer = new();
        int trainCorpusSize;
        double confidence;
        using ITrainer smtModelTrainer = await _smtModelFactory.CreateTrainerAsync(
            engineDir,
            tokenizer,
            parallelCorpus,
            cancellationToken
        );
        using ITrainer truecaseTrainer = await _truecaserFactory.CreateTrainerAsync(
            engineDir,
            tokenizer,
            targetCorpus,
            cancellationToken
        );
        cancellationToken.ThrowIfCancellationRequested();

        var progress = new BuildProgress(PlatformService, buildId);
        await smtModelTrainer.TrainAsync(progress, cancellationToken);
        await truecaseTrainer.TrainAsync(cancellationToken: cancellationToken);

        trainCorpusSize = smtModelTrainer.Stats.TrainCorpusSize;
        confidence = smtModelTrainer.Stats.Metrics["bleu"] * 100.0;

        cancellationToken.ThrowIfCancellationRequested();

        await smtModelTrainer.SaveAsync(cancellationToken);
        await truecaseTrainer.SaveAsync(cancellationToken);

        await using Stream engineStream = await _sharedFileService.OpenWriteAsync(
            $"builds/{buildId}/model.tar.gz",
            cancellationToken
        );
        await _smtModelFactory.SaveEngineToAsync(engineDir, engineStream, cancellationToken);
        return (trainCorpusSize, confidence);
    }

    private async Task GeneratePretranslationsAsync(
        string buildId,
        string engineDir,
        CancellationToken cancellationToken
    )
    {
        await using Stream sourceStream = await _sharedFileService.OpenReadAsync(
            $"builds/{buildId}/pretranslate.src.json",
            cancellationToken
        );

        IAsyncEnumerable<Pretranslation> pretranslations = JsonSerializer
            .DeserializeAsyncEnumerable<Pretranslation>(sourceStream, JsonSerializerOptions, cancellationToken)
            .OfType<Pretranslation>();

        await using Stream targetStream = await _sharedFileService.OpenWriteAsync(
            $"builds/{buildId}/pretranslate.trg.json",
            cancellationToken
        );
        await using Utf8JsonWriter targetWriter = new(targetStream, PretranslateWriterOptions);

        LatinWordTokenizer tokenizer = new();
        LatinWordDetokenizer detokenizer = new();
        ITruecaser truecaser = await _truecaserFactory.CreateAsync(engineDir, CancellationToken.None);
        using IInteractiveTranslationModel smtModel = await _smtModelFactory.CreateAsync(
            engineDir,
            tokenizer,
            detokenizer,
            truecaser,
            cancellationToken
        );
        using ITranslationEngine? transferEngine = await _transferEngineFactory.CreateAsync(
            engineDir,
            tokenizer,
            detokenizer,
            truecaser,
            cancellationToken
        );
        HybridTranslationEngine hybridEngine = new(smtModel, transferEngine) { TargetDetokenizer = detokenizer };

        await foreach (IReadOnlyList<Pretranslation> batch in BatchAsync(pretranslations))
        {
            string[] segments = batch.Select(p => p.Translation).ToArray();
            IReadOnlyList<TranslationResult> results = await hybridEngine.TranslateBatchAsync(
                segments,
                cancellationToken
            );
            foreach ((Pretranslation pretranslation, TranslationResult result) in batch.Zip(results))
            {
                JsonSerializer.Serialize(
                    targetWriter,
                    pretranslation with
                    {
                        Translation = result.Translation
                    },
                    JsonSerializerOptions
                );
            }
        }
    }

    public static async IAsyncEnumerable<IReadOnlyList<Pretranslation>> BatchAsync(
        IAsyncEnumerable<Pretranslation> pretranslations
    )
    {
        List<Pretranslation> batch = [];
        await foreach (Pretranslation item in pretranslations)
        {
            batch.Add(item);
            if (batch.Count == BatchSize)
            {
                yield return batch;
                batch = [];
            }
        }
        if (batch.Count > 0)
            yield return batch;
    }
}
