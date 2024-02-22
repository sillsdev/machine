namespace SIL.Machine.AspNetCore.Services;

public class NmtPreprocessBuildJob : HangfireBuildJob<IReadOnlyList<Corpus>>
{
    private static readonly JsonWriterOptions PretranslateWriterOptions = new() { Indented = true };

    private readonly ISharedFileService _sharedFileService;
    private readonly ICorpusService _corpusService;
    private readonly ILanguageTagService _languageTagService;
    private int _seed = 1234;
    private Random _random;

    public NmtPreprocessBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        IDistributedReaderWriterLockFactory lockFactory,
        ILogger<NmtPreprocessBuildJob> logger,
        IBuildJobService buildJobService,
        ISharedFileService sharedFileService,
        ICorpusService corpusService,
        ILanguageTagService languageTagService
    )
        : base(platformService, engines, lockFactory, buildJobService, logger)
    {
        _sharedFileService = sharedFileService;
        _corpusService = corpusService;
        _languageTagService = languageTagService;
        _random = new Random(_seed);
    }

    internal int Seed
    {
        get => _seed;
        set
        {
            if (_seed != value)
            {
                _seed = value;
                _random = new Random(_seed);
            }
        }
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
        await using Stream pretranslateStream = await _sharedFileService.OpenWriteAsync(
            $"builds/{buildId}/pretranslate.src.json",
            cancellationToken
        );
        await using Utf8JsonWriter pretranslateWriter = new(pretranslateStream, PretranslateWriterOptions);

        int trainCount = 0;
        int pretranslateCount = 0;
        pretranslateWriter.WriteStartArray();
        foreach (Corpus corpus in corpora)
        {
            ITextCorpus[] sourceTextCorpora = _corpusService.CreateTextCorpora(corpus.SourceFiles).ToArray();
            ITextCorpus? targetTextCorpus = _corpusService.CreateTextCorpora(corpus.TargetFiles).FirstOrDefault();
            if (sourceTextCorpora.Length == 0 || targetTextCorpus is null)
                continue;

            int skipCount = 0;
            foreach (Row?[] rows in AlignCorpora(sourceTextCorpora, targetTextCorpus))
            {
                if (skipCount > 0)
                {
                    skipCount--;
                    continue;
                }

                Row[] trainRows = rows.Where(r => r is not null && IsInTrain(r, corpus)).Cast<Row>().ToArray();
                if (trainRows.Length > 0)
                {
                    Row row = trainRows[0];
                    if (rows.Length > 1)
                    {
                        Row[] nonEmptyRows = trainRows.Where(r => r.SourceSegment.Length > 0).ToArray();
                        if (nonEmptyRows.Length > 0)
                            row = nonEmptyRows[_random.Next(nonEmptyRows.Length)];
                    }

                    await sourceTrainWriter.WriteAsync($"{row.SourceSegment}\n");
                    await targetTrainWriter.WriteAsync($"{row.TargetSegment}\n");
                    skipCount = row.RowCount - 1;
                    if (row.SourceSegment.Length > 0 && row.TargetSegment.Length > 0)
                        trainCount++;
                }

                Row? pretranslateRow = rows[0];
                if (
                    pretranslateRow is not null
                    && IsInPretranslate(pretranslateRow, corpus)
                    && pretranslateRow.SourceSegment.Length > 0
                    && pretranslateRow.TargetSegment.Length == 0
                )
                {
                    pretranslateWriter.WriteStartObject();
                    pretranslateWriter.WriteString("corpusId", corpus.Id);
                    pretranslateWriter.WriteString("textId", pretranslateRow.TextId);
                    pretranslateWriter.WriteStartArray("refs");
                    foreach (object rowRef in pretranslateRow.Refs)
                        pretranslateWriter.WriteStringValue(rowRef.ToString());
                    pretranslateWriter.WriteEndArray();
                    pretranslateWriter.WriteString("translation", pretranslateRow.SourceSegment);
                    pretranslateWriter.WriteEndObject();
                    pretranslateCount++;
                }
            }

            if ((bool?)buildOptionsObject?["use_key_terms"] ?? true)
            {
                ITextCorpus? sourceTermCorpus = _corpusService.CreateTermCorpora(corpus.SourceFiles).FirstOrDefault();
                ITextCorpus? targetTermCorpus = _corpusService.CreateTermCorpora(corpus.TargetFiles).FirstOrDefault();
                if (sourceTermCorpus is not null && targetTermCorpus is not null)
                {
                    IParallelTextCorpus parallelKeyTermsCorpus = sourceTermCorpus.AlignRows(targetTermCorpus);
                    foreach (ParallelTextRow row in parallelKeyTermsCorpus)
                    {
                        await sourceTrainWriter.WriteAsync($"{row.SourceText}\n");
                        await targetTrainWriter.WriteAsync($"{row.TargetText}\n");
                        trainCount++;
                    }
                }
            }
        }
        pretranslateWriter.WriteEndArray();

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
                Logger.LogWarning(e, "Unable to to delete job data for build {BuildId}.", buildId);
            }
        }
    }

    private static bool IsInTrain(Row row, Corpus corpus)
    {
        return IsIncluded(row, corpus.TrainOnAll, corpus.TrainOnTextIds, corpus.TrainOnChapters);
    }

    private static bool IsInPretranslate(Row row, Corpus corpus)
    {
        return IsIncluded(row, corpus.PretranslateAll, corpus.PretranslateTextIds, corpus.PretranslateChapters);
    }

    private static bool IsIncluded(
        Row row,
        bool all,
        IReadOnlySet<string> textIds,
        IReadOnlyDictionary<string, HashSet<int>>? chapters
    )
    {
        if (chapters is not null)
        {
            if (row.Refs.Any(r => IsInChapters(chapters, r)))
                return true;
        }
        return all || textIds.Contains(row.TextId);
    }

    private static bool IsInChapters(IReadOnlyDictionary<string, HashSet<int>> bookChapters, object rowRef)
    {
        if (rowRef is not VerseRef vr)
            return false;
        return bookChapters.TryGetValue(vr.Book, out HashSet<int>? chapters)
            && (chapters.Contains(vr.ChapterNum) || chapters.Count == 0);
    }

    private static IEnumerable<Row?[]> AlignCorpora(IReadOnlyList<ITextCorpus> srcCorpora, ITextCorpus trgCorpus)
    {
        if (trgCorpus.IsScripture())
        {
            return srcCorpora
                .Select(sc => AlignScripture(sc, trgCorpus))
                .ZipMany(rows => rows.ToArray())
                // filter out every list that only contains completely empty rows
                .Where(rows => rows.Any(r => r is null || r.SourceSegment.Length > 0 || r.TargetSegment.Length > 0));
        }

        IEnumerable<Row[]> sourceOnlyRows = srcCorpora
            .Select(sc => sc.AlignRows(trgCorpus, allSourceRows: true))
            .ZipMany(rows =>
                rows.Where(r => r.TargetSegment.Count == 0)
                    .Select(r => new Row(r.TextId, r.Refs, r.SourceText, r.TargetText, 1))
                    .ToArray()
            );

        IEnumerable<Row[]> targetRows = srcCorpora
            .Select(sc => sc.AlignRows(trgCorpus, allTargetRows: true))
            .ZipMany(rows =>
                rows.Where(r => r.TargetSegment.Count > 0)
                    .Select(r => new Row(r.TextId, r.Refs, r.SourceText, r.TargetText, 1))
                    .ToArray()
            );

        return sourceOnlyRows
            .Concat(targetRows)
            // filter out every list that only contains completely empty rows
            .Where(rows => rows.Any(r => r.SourceSegment.Length > 0 || r.TargetSegment.Length > 0));
    }

    private static IEnumerable<Row?> AlignScripture(ITextCorpus srcCorpus, ITextCorpus trgCorpus)
    {
        int rowCount = 0;
        StringBuilder srcSegBuffer = new();
        StringBuilder trgSegBuffer = new();
        HashSet<VerseRef> vrefs = [];
        foreach (
            (VerseRef vref, string srcSegment, string trgSegment) in srcCorpus
                .ExtractScripture()
                .Select(r => (r.CorpusVerseRef, r.Text))
                .Zip(
                    trgCorpus.ExtractScripture().Select(r => r.Text),
                    (s, t) => (VerseRef: s.CorpusVerseRef, SourceSegment: s.Text, TargetSegment: t)
                )
        )
        {
            if (srcSegment == "<range>" && trgSegment == "<range>")
            {
                vrefs.UnionWith(vref.AllVerses());
                rowCount++;
            }
            else if (srcSegment == "<range>")
            {
                vrefs.UnionWith(vref.AllVerses());
                if (trgSegment.Length > 0)
                {
                    if (trgSegBuffer.Length > 0)
                        trgSegBuffer.Append(' ');
                    trgSegBuffer.Append(trgSegment);
                }
                rowCount++;
            }
            else if (trgSegment == "<range>")
            {
                vrefs.UnionWith(vref.AllVerses());
                if (srcSegment.Length > 0)
                {
                    if (srcSegBuffer.Length > 0)
                        srcSegBuffer.Append(' ');
                    srcSegBuffer.Append(srcSegment);
                }
                rowCount++;
            }
            else
            {
                if (rowCount > 0)
                {
                    yield return new(
                        vrefs.First().Book,
                        vrefs.Order().Cast<object>().ToArray(),
                        srcSegBuffer.ToString(),
                        trgSegBuffer.ToString(),
                        rowCount
                    );
                    for (int i = 0; i < rowCount - 1; i++)
                        yield return null;
                    srcSegBuffer.Clear();
                    trgSegBuffer.Clear();
                    vrefs.Clear();
                    rowCount = 0;
                }
                vrefs.UnionWith(vref.AllVerses());
                srcSegBuffer.Append(srcSegment);
                trgSegBuffer.Append(trgSegment);
                rowCount++;
            }
        }

        if (rowCount > 0)
        {
            yield return new(
                vrefs.First().Book,
                vrefs.Order().Cast<object>().ToArray(),
                srcSegBuffer.ToString(),
                trgSegBuffer.ToString(),
                rowCount
            );
            for (int i = 0; i < rowCount - 1; i++)
                yield return null;
        }
    }

    private record Row(
        string TextId,
        IReadOnlyList<object> Refs,
        string SourceSegment,
        string TargetSegment,
        int RowCount
    );
}
