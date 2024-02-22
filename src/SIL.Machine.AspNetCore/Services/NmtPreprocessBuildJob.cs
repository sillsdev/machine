namespace SIL.Machine.AspNetCore.Services;

public class NmtPreprocessBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    ILogger<NmtPreprocessBuildJob> logger,
    IBuildJobService buildJobService,
    ISharedFileService sharedFileService,
    ICorpusService corpusService,
    ILanguageTagService languageTagService
) : HangfireBuildJob<IReadOnlyList<Corpus>>(platformService, engines, lockFactory, buildJobService, logger)
{
    private static readonly JsonSerializerOptions PretranslateSerializerOptions =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ISharedFileService _sharedFileService = sharedFileService;
    private readonly ICorpusService _corpusService = corpusService;
    private readonly ILanguageTagService _languageTagService = languageTagService;

    protected override async Task DoWorkAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> data,
        string? buildOptions,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        (int trainCount, int pretranslateCount) = await WriteDataFilesAsync(
            buildId,
            data,
            buildOptions,
            cancellationToken
        );

        // Log summary of build data
        JsonObject buildPreprocessSummary =
            new()
            {
                { "Event", "BuildPreprocess" },
                { "EngineId", engineId },
                { "BuildId", buildId },
                { "NumTrainRows", trainCount },
                { "NumPretranslateRows", pretranslateCount }
            };
        TranslationEngine? engine = await Engines.GetAsync(e => e.EngineId == engineId, cancellationToken);
        if (engine is null)
            throw new OperationCanceledException($"Engine {engineId} does not exist.  Build canceled.");

        _languageTagService.ConvertToFlores200Code(engine.SourceLanguage, out string srcLang);
        buildPreprocessSummary.Add("SourceLanguageResolved", srcLang);
        _languageTagService.ConvertToFlores200Code(engine.TargetLanguage, out string trgLang);
        buildPreprocessSummary.Add("TargetLanguageResolved", trgLang);
        Logger.LogInformation("{summary}", buildPreprocessSummary.ToJsonString());

        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            bool canceling = !await BuildJobService.StartBuildJobAsync(
                BuildJobType.Gpu,
                TranslationEngineType.Nmt,
                engineId,
                buildId,
                NmtBuildStages.Train,
                buildOptions: buildOptions,
                cancellationToken: cancellationToken
            );
            if (canceling)
                throw new OperationCanceledException();
        }
    }

    private async Task<(int TrainCount, int PretranslateCount)> WriteDataFilesAsync(
        string buildId,
        IReadOnlyList<Corpus> corpora,
        string? buildOptions,
        CancellationToken cancellationToken
    )
    {
        JsonObject? buildOptionsObject = null;
        if (buildOptions is not null)
        {
            buildOptionsObject = JsonSerializer.Deserialize<JsonObject>(buildOptions);
        }
        await using StreamWriter sourceTrainWriter =
            new(await _sharedFileService.OpenWriteAsync($"builds/{buildId}/train.src.txt", cancellationToken));
        await using StreamWriter targetTrainWriter =
            new(await _sharedFileService.OpenWriteAsync($"builds/{buildId}/train.trg.txt", cancellationToken));

        int trainCount = 0;
        int pretranslateCount = 0;
        async IAsyncEnumerable<Pretranslation> ProcessRowsAsync()
        {
            foreach (Corpus corpus in corpora)
            {
                IDictionary<CorpusType, ITextCorpus> sourceCorpora = _corpusService.CreateTextCorpus(
                    corpus.SourceFiles
                );
                IDictionary<CorpusType, ITextCorpus> targetCorpora = _corpusService.CreateTextCorpus(
                    corpus.TargetFiles
                );

                IParallelTextCorpus parallelTextCorpus = sourceCorpora[CorpusType.Text]
                    .AlignRows(targetCorpora[CorpusType.Text], allSourceRows: true, allTargetRows: true);
                foreach (ParallelTextRow row in parallelTextCorpus)
                {
                    bool isInTrainOnChapters = false;
                    bool isInPretranslateChapters = false;
                    if (targetCorpora[CorpusType.Text] is ScriptureTextCorpus stc)
                    {
                        bool IsInChapters(IReadOnlyDictionary<string, IReadOnlySet<int>> bookChapters, object rowRef)
                        {
                            if (rowRef is not VerseRef vr)
                                return false;
                            return bookChapters.TryGetValue(vr.Book, out IReadOnlySet<int>? chapters)
                                && (chapters.Contains(vr.ChapterNum) || chapters.Count == 0);
                        }
                        if (corpus.TrainOnChapters is not null)
                            isInTrainOnChapters = row.Refs.Any(r => IsInChapters(corpus.TrainOnChapters, r));
                        if (corpus.PretranslateChapters is not null)
                            isInPretranslateChapters = row.Refs.Any(r => IsInChapters(corpus.PretranslateChapters, r));
                    }
                    if (corpus.TrainOnAll || corpus.TrainOnTextIds.Contains(row.TextId) || isInTrainOnChapters)
                    {
                        await sourceTrainWriter.WriteAsync($"{row.SourceText}\n");
                        await targetTrainWriter.WriteAsync($"{row.TargetText}\n");
                        if (!row.IsEmpty)
                            trainCount++;
                    }
                    if (
                        (
                            corpus.PretranslateAll
                            || corpus.PretranslateTextIds.Contains(row.TextId)
                            || isInPretranslateChapters
                        )
                        && row.SourceSegment.Count > 0
                        && row.TargetSegment.Count == 0
                    )
                    {
                        IReadOnlyList<object> refs;
                        if (row.TargetRefs.Count == 0)
                        {
                            refs = row.SourceRefs;
                        }
                        else
                        {
                            refs = row.TargetRefs;
                        }
                        yield return new Pretranslation
                        {
                            CorpusId = corpus.Id,
                            TextId = row.TextId,
                            Refs = refs.Select(r => r.ToString() ?? "").ToList(),
                            Translation = row.SourceText
                        };
                        pretranslateCount++;
                    }
                }

                if (
                    (bool?)buildOptionsObject?["use_key_terms"]
                    ?? true && sourceCorpora.ContainsKey(CorpusType.Term) && targetCorpora.ContainsKey(CorpusType.Term)
                )
                {
                    IParallelTextCorpus parallelKeyTermsCorpus = sourceCorpora[CorpusType.Term]
                        .AlignRows(targetCorpora[CorpusType.Term]);
                    foreach (ParallelTextRow row in parallelKeyTermsCorpus)
                    {
                        await sourceTrainWriter.WriteAsync($"{row.SourceText}\n");
                        await targetTrainWriter.WriteAsync($"{row.TargetText}\n");
                        trainCount++;
                    }
                }
            }
        }

        await using var sourcePretranslateStream = await _sharedFileService.OpenWriteAsync(
            $"builds/{buildId}/pretranslate.src.json",
            cancellationToken
        );

        await JsonSerializer.SerializeAsync(
            sourcePretranslateStream,
            ProcessRowsAsync(),
            PretranslateSerializerOptions,
            cancellationToken: cancellationToken
        );

        return (trainCount, pretranslateCount);
    }

    protected override async Task CleanupAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> data,
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
                Logger.LogWarning(e, "Unable to to delete job data for build {0}.", buildId);
            }
        }
    }
}
