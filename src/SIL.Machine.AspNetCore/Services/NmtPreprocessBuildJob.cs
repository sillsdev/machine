namespace SIL.Machine.AspNetCore.Services;

public class NmtPreprocessBuildJob : HangfireBuildJob<IReadOnlyList<Corpus>>
{
    private readonly ISharedFileService _sharedFileService;
    private readonly ICorpusService _corpusService;

    public NmtPreprocessBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        IDistributedReaderWriterLockFactory lockFactory,
        ILogger<NmtPreprocessBuildJob> logger,
        IBuildJobService buildJobService,
        ISharedFileService sharedFileService,
        ICorpusService corpusService
    )
        : base(platformService, engines, lockFactory, buildJobService, logger)
    {
        _sharedFileService = sharedFileService;
        _corpusService = corpusService;
    }

    protected override async Task DoWorkAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> data,
        string? buildOptions,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        await WriteDataFilesAsync(buildId, data, cancellationToken);

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

    private async Task<int> WriteDataFilesAsync(
        string buildId,
        IReadOnlyList<Corpus> corpora,
        CancellationToken cancellationToken
    )
    {
        await using var sourceTrainWriter = new StreamWriter(
            await _sharedFileService.OpenWriteAsync($"builds/{buildId}/train.src.txt", cancellationToken)
        );
        await using var targetTrainWriter = new StreamWriter(
            await _sharedFileService.OpenWriteAsync($"builds/{buildId}/train.trg.txt", cancellationToken)
        );

        int corpusSize = 0;
        async IAsyncEnumerable<Pretranslation> ProcessRowsAsync()
        {
            foreach (Corpus corpus in corpora)
            {
                ITextCorpus sourceCorpus = _corpusService.CreateTextCorpus(corpus.SourceFiles);
                ITextCorpus targetCorpus = _corpusService.CreateTextCorpus(corpus.TargetFiles);

                IParallelTextCorpus parallelCorpus = sourceCorpus.AlignRows(
                    targetCorpus,
                    allSourceRows: true,
                    allTargetRows: true
                );

                foreach (ParallelTextRow row in parallelCorpus)
                {
                    if (corpus.TrainOnAll || corpus.TrainOnTextIds.Contains(row.TextId))
                    {
                        await sourceTrainWriter.WriteAsync($"{row.SourceText}\n");
                        await targetTrainWriter.WriteAsync($"{row.TargetText}\n");
                    }
                    if (
                        (corpus.PretranslateAll || corpus.PretranslateTextIds.Contains(row.TextId))
                        && row.SourceSegment.Count > 0
                        && row.TargetSegment.Count == 0
                    )
                    {
                        IReadOnlyList<object> refs;
                        if (row.TargetRefs.Count == 0)
                        {
                            if (targetCorpus is ScriptureTextCorpus tstc)
                            {
                                refs = row.SourceRefs
                                    .Cast<VerseRef>()
                                    .Select(srcRef =>
                                    {
                                        var trgRef = srcRef.Clone();
                                        trgRef.ChangeVersification(tstc.Versification);
                                        return (object)trgRef;
                                    })
                                    .ToList();
                            }
                            else
                            {
                                refs = row.SourceRefs;
                            }
                        }
                        else
                        {
                            refs = row.TargetRefs;
                        }
                        yield return new Pretranslation
                        {
                            CorpusId = corpus.Id,
                            TextId = row.TextId,
                            Refs = refs.Select(r => r.ToString()!).ToList(),
                            Translation = row.SourceText
                        };
                    }
                    if (!row.IsEmpty)
                        corpusSize++;
                }
                if (
                    corpus.SourceFiles.Count() == 1
                    && corpus.SourceFiles.First().Format == FileFormat.Paratext
                    && corpus.TargetFiles.Count() == 1
                    && corpus.TargetFiles.First().Format == FileFormat.Paratext
                )
                {
                    try
                    {
                        ITextCorpus keyTermsSourceCorpus = new ParatextKeyTermsCorpus(
                            corpus.SourceFiles.First().Location
                        );
                        ITextCorpus keyTermsTargetCorpus = new ParatextKeyTermsCorpus(
                            corpus.TargetFiles.First().Location
                        );
                        IParallelTextCorpus parallelKeyTermsCorpus = keyTermsSourceCorpus.AlignRows(
                            keyTermsTargetCorpus
                        );
                        foreach (ParallelTextRow row in parallelKeyTermsCorpus)
                        {
                            await sourceTrainWriter.WriteAsync($"{row.SourceText}\n");
                            await targetTrainWriter.WriteAsync($"{row.TargetText}\n");
                        }
                    }
                    catch (ArgumentException)
                    {
                        //No key terms file - not an error
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
            new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
            cancellationToken: cancellationToken
        );
        return corpusSize;
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
