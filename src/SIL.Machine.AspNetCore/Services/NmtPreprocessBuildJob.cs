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

        bool sourceTagInFlores200 = _languageTagService.ConvertToFlores200Code(
            engine.SourceLanguage,
            out string srcLang
        );
        buildPreprocessSummary.Add("SourceLanguageResolved", srcLang);
        bool targetTagInFlores200 = _languageTagService.ConvertToFlores200Code(
            engine.TargetLanguage,
            out string trgLang
        );
        buildPreprocessSummary.Add("TargetLanguageResolved", trgLang);
        Logger.LogInformation("{summary}", buildPreprocessSummary.ToJsonString());

        if (trainCount == 0 && (!sourceTagInFlores200 || !targetTagInFlores200))
        {
            throw new InvalidOperationException(
                $"Neither language code in build {buildId} are known to the base model, and the data specified for training was empty. Build canceled."
            );
        }

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
            ITextCorpus targetTextCorpus =
                _corpusService.CreateTextCorpora(corpus.TargetFiles).FirstOrDefault() ?? new DictionaryTextCorpus();

            if (sourceTextCorpora.Length == 0)
                continue;

            int skipCount = 0;
            foreach (Row?[] rows in AlignTrainCorpus(corpus, sourceTextCorpora, targetTextCorpus))
            {
                if (skipCount > 0)
                {
                    skipCount--;
                    continue;
                }

                Row[] trainRows = rows.Where(r => IsIncluded(r, corpus.TrainOnChapters)).Cast<Row>().ToArray();
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

            foreach (Row row in AlignPretranslateCorpus(corpus, sourceTextCorpora[0], targetTextCorpus))
            {
                if (
                    IsIncluded(row, corpus.PretranslateChapters)
                    && row.SourceSegment.Length > 0
                    && (row.TargetSegment.Length == 0 || !IsInTrain(row, corpus))
                )
                {
                    pretranslateWriter.WriteStartObject();
                    pretranslateWriter.WriteString("corpusId", corpus.Id);
                    pretranslateWriter.WriteString("textId", row.TextId);
                    pretranslateWriter.WriteStartArray("refs");
                    foreach (object rowRef in row.Refs)
                        pretranslateWriter.WriteStringValue(rowRef.ToString());
                    pretranslateWriter.WriteEndArray();
                    pretranslateWriter.WriteString("translation", row.SourceSegment);
                    pretranslateWriter.WriteEndObject();
                    pretranslateCount++;
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
        if (corpus.TrainOnChapters is not null)
        {
            if (row.Refs.Any(r => IsInChapters(corpus.TrainOnChapters, r)))
                return true;
        }
        return corpus.TrainOnAll || corpus.TrainOnTextIds.Contains(row.TextId);
    }

    private static bool IsIncluded(Row? row, IReadOnlyDictionary<string, HashSet<int>>? chapters)
    {
        if (row is null)
            return false;
        if (chapters is not null)
            return row.Refs.Any(r => IsInChapters(chapters, r));
        return true;
    }

    private static bool IsInChapters(IReadOnlyDictionary<string, HashSet<int>> bookChapters, object rowRef)
    {
        if (rowRef is not ScriptureRef sr)
            return false;
        return bookChapters.TryGetValue(sr.Book, out HashSet<int>? chapters)
            && (chapters.Contains(sr.ChapterNum) || chapters.Count == 0);
    }

    private static IEnumerable<Row?[]> AlignTrainCorpus(
        Corpus corpus,
        IReadOnlyList<ITextCorpus> srcCorpora,
        ITextCorpus trgCorpus
    )
    {
        if (!corpus.TrainOnAll)
        {
            IEnumerable<string> textIds = corpus.TrainOnChapters is not null
                ? corpus.TrainOnChapters.Keys
                : corpus.TrainOnTextIds;
            srcCorpora = srcCorpora.Select(sc => sc.FilterTexts(textIds)).ToArray();
            trgCorpus = trgCorpus.FilterTexts(textIds);
        }

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
                        vrefs.Order().Select(v => new ScriptureRef(v)).Cast<object>().ToArray(),
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
                vrefs.Order().Select(v => new ScriptureRef(v)).Cast<object>().ToArray(),
                srcSegBuffer.ToString(),
                trgSegBuffer.ToString(),
                rowCount
            );
            for (int i = 0; i < rowCount - 1; i++)
                yield return null;
        }
    }

    private static IEnumerable<Row> AlignPretranslateCorpus(Corpus corpus, ITextCorpus srcCorpus, ITextCorpus trgCorpus)
    {
        if (!corpus.PretranslateAll)
        {
            IEnumerable<string> textIds = corpus.PretranslateChapters is not null
                ? corpus.PretranslateChapters.Keys
                : corpus.PretranslateTextIds;
            srcCorpus = srcCorpus.FilterTexts(textIds);
            trgCorpus = trgCorpus.FilterTexts(textIds);
        }

        int rowCount = 0;
        StringBuilder srcSegBuffer = new();
        StringBuilder trgSegBuffer = new();
        List<object> refs = [];
        string textId = "";
        foreach (ParallelTextRow row in srcCorpus.AlignRows(trgCorpus, allSourceRows: true))
        {
            if (!row.IsTargetRangeStart && row.IsTargetInRange)
            {
                refs.AddRange(row.Refs);
                if (row.SourceText.Length > 0)
                {
                    if (srcSegBuffer.Length > 0)
                        srcSegBuffer.Append(' ');
                    srcSegBuffer.Append(row.SourceText);
                }
                rowCount++;
            }
            else
            {
                if (rowCount > 0)
                {
                    yield return new(textId, refs, srcSegBuffer.ToString(), trgSegBuffer.ToString(), 1);
                    textId = "";
                    srcSegBuffer.Clear();
                    trgSegBuffer.Clear();
                    refs.Clear();
                    rowCount = 0;
                }

                textId = row.TextId;
                refs.AddRange(row.Refs);
                srcSegBuffer.Append(row.SourceText);
                trgSegBuffer.Append(row.TargetText);
                rowCount++;
            }
        }

        if (rowCount > 0)
            yield return new(textId, refs, srcSegBuffer.ToString(), trgSegBuffer.ToString(), 1);
    }

    private record Row(
        string TextId,
        IReadOnlyList<object> Refs,
        string SourceSegment,
        string TargetSegment,
        int RowCount
    );
}
