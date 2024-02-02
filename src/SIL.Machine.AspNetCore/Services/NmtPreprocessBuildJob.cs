using System.Data;
using Google.Protobuf;
using MongoDB.Bson.IO;

namespace SIL.Machine.AspNetCore.Services;

public class NmtPreprocessBuildJob : HangfireBuildJob<IReadOnlyList<Corpus>>
{
    private readonly ISharedFileService _sharedFileService;
    private readonly ICorpusService _corpusService;
    private readonly ILanguageTagService _languageTagService;

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
        IDictionary<string, int> counts = await WriteDataFilesAsync(buildId, data, buildOptions, cancellationToken);

        // Log summary of build data
        JsonObject _buildPreprocessSummary =
            new()
            {
                { "Event", "BuildPreprocess" },
                { "EngineId", engineId },
                { "BuildId", buildId }
            };
        foreach (KeyValuePair<string, int> kvp in counts)
        {
            _buildPreprocessSummary.Add(kvp.Key, kvp.Value);
        }
        TranslationEngine? engine = await Engines.GetAsync(e => e.EngineId == engineId, cancellationToken);
        if (engine is null)
            throw new OperationCanceledException($"Engine {engineId} does not exist.  Build canceled.");

        _languageTagService.ConvertToFlores200Code(engine.SourceLanguage, out string srcLang);
        _buildPreprocessSummary.Add("SourceLanguageResolved", srcLang);
        _languageTagService.ConvertToFlores200Code(engine.TargetLanguage, out string trgLang);
        _buildPreprocessSummary.Add("TargetLanguageResolved", trgLang);
        Logger.LogInformation("{summary}", _buildPreprocessSummary.ToJsonString());

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

    private async Task<IDictionary<string, int>> WriteDataFilesAsync(
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
        await using var sourceTrainWriter = new StreamWriter(
            await _sharedFileService.OpenWriteAsync($"builds/{buildId}/train.src.txt", cancellationToken)
        );
        await using var targetTrainWriter = new StreamWriter(
            await _sharedFileService.OpenWriteAsync($"builds/{buildId}/train.trg.txt", cancellationToken)
        );

        Dictionary<string, int> counts = new();
        counts["CorpusSize"] = 0;
        counts["NumTrainRows"] = 0;
        counts["NumPretranslateRows"] = 0;
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

                var parallelCorpora = new List<IParallelTextCorpus>();

                IParallelTextCorpus parallelTextCorpus = sourceCorpora[CorpusType.Text]
                    .AlignRows(targetCorpora[CorpusType.Text], allSourceRows: true, allTargetRows: true);
                parallelCorpora.Add(parallelTextCorpus);
                if (
                    (bool?)buildOptionsObject?["use_key_terms"]
                    ?? true && sourceCorpora.ContainsKey(CorpusType.Term) && targetCorpora.ContainsKey(CorpusType.Term)
                )
                {
                    IParallelTextCorpus parallelKeyTermsCorpus = sourceCorpora[CorpusType.Term]
                        .AlignRows(targetCorpora[CorpusType.Term]);
                    IEnumerable<string> keyTermsTextIds = parallelKeyTermsCorpus.Select(r => r.TextId).Distinct();
                    if (keyTermsTextIds.Count() == 1)
                        corpus.TrainOnTextIds.Add(keyTermsTextIds.First()); //Should only be one textId for key terms
                    parallelCorpora.Add(parallelKeyTermsCorpus);
                }

                foreach (ParallelTextRow row in parallelCorpora.Flatten())
                {
                    bool isInTrainOnRange = false;
                    bool isInPretranslateRange = false;
                    if(targetCorpora[CorpusType.Text] is ScriptureTextCorpus stc && row.Refs.All(r => r is VerseRef)){
                        Dictionary<string, List<int>> rowChaptersPerBook = row.Refs.Cast<VerseRef>().GroupBy(vr => vr.Book).ToDictionary(g => g.Key, g => g.Select(vr => vr.ChapterNum).ToList());
                        var parser = new BiblicalRangeStringParser(stc.Versification);
                        if(corpus.TrainOnBiblicalRange != null && corpus.TrainOnBiblicalRange != ""){
                            Dictionary<string, List<int>> trainOnBiblicalRangeChapters = parser.Parse(corpus.TrainOnBiblicalRange); //TODO calculate once
                            isInTrainOnRange = rowChaptersPerBook.Join(trainOnBiblicalRangeChapters, rcpb => rcpb.Key, tobrc => tobrc.Key, (rcbp, tobrc) =>
                                rcbp.Value.Intersect(tobrc.Value).Count() > 0 || (rcbp.Value.Count() > 0 && tobrc.Value.Count() == 0) //Empty list means all chapters from book
                            ).Any(b => b);
                        }
                        if(corpus.PretranslateBiblicalRange != null && corpus.PretranslateBiblicalRange != ""){
                            Dictionary<string, List<int>> pretranslateBiblicalRangeChapters = parser.Parse(corpus.PretranslateBiblicalRange);
                            isInPretranslateRange = rowChaptersPerBook.Join(pretranslateBiblicalRangeChapters, rcpb => rcpb.Key, pbrc => pbrc.Key, (rcbp, pbrc) =>
                                rcbp.Value.Intersect(pbrc.Value).Count() > 0 || (rcbp.Value.Count() > 0 && pbrc.Value.Count() == 0)
                            ).Any(b => b);
                        }
                    }
                    if (corpus.TrainOnAll || corpus.TrainOnTextIds.Contains(row.TextId) || isInTrainOnRange)
                    {
                        await sourceTrainWriter.WriteAsync($"{row.SourceText}\n");
                        await targetTrainWriter.WriteAsync($"{row.TargetText}\n");
                        counts["NumTrainRows"] += 1;
                    }
                    if (
                        (corpus.PretranslateAll || corpus.PretranslateTextIds.Contains(row.TextId) || isInPretranslateRange)
                        && row.SourceSegment.Count > 0
                        && row.TargetSegment.Count == 0
                    )
                    {
                        IReadOnlyList<object> refs;
                        if (row.TargetRefs.Count == 0)
                        {
                            if (targetCorpora[CorpusType.Text] is ScriptureTextCorpus tstc)
                            {
                                refs = row
                                    .SourceRefs.Cast<VerseRef>()
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
                        counts["NumPretranslateRows"] += 1;
                        yield return new Pretranslation
                        {
                            CorpusId = corpus.Id,
                            TextId = row.TextId,
                            Refs = refs.Select(r => r.ToString()!).ToList(),
                            Translation = row.SourceText
                        };
                    }
                    if (!row.IsEmpty)
                        counts["CorpusSize"]++;
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

        return counts;
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
