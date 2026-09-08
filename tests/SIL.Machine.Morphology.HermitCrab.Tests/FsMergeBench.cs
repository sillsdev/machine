using System.Diagnostics;
using System.Text.Json;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// PR #494 measurement harness ("Change Add to PriorityUnion" in HermitCrab analysis). For each word in a
/// real corpus, resets <see cref="AnalysisSyntacticFeatureMerge"/>'s deterministic step counters, parses the
/// word under whatever mode the process was started with (env <c>HC_ANALYSIS_FS_MERGE</c>), and appends one
/// JSON line recording timing, counters, and a parity signature (the sorted set of per-analysis strings) to
/// <c>HC_FSM_OUT</c>.
/// <para>
/// [Explicit] and env-var driven for the same reason as <see cref="MemoCorpusVerification"/>: this repo never
/// commits real grammars or word lists, so the test embeds no grammar content and the jsonl it writes (which
/// does contain morpheme glosses and surface forms) must land only under an uncommitted scratch path, never
/// in the repo. Drive it via scripts/fs-merge-bench.ps1, which sets <c>HC_ANALYSIS_FS_MERGE</c> per mode and
/// aggregates the resulting jsonl files into a report that names only word indices and counts.
/// </para>
/// <code>
///   $env:HC_ANALYSIS_FS_MERGE = "PriorityUnion"   # Add | PriorityUnion | Exact
///   $env:HC_FSM_GRAMMAR = "...\sena-hc.xml"
///   $env:HC_FSM_WORDS   = "...\sena-words.txt"
///   $env:HC_FSM_MAX_WORDS = "30"                  # optional, default 30
///   $env:HC_FSM_TIMEOUT_MS = "180000"             # optional, default 180000 (per-word watchdog)
///   $env:HC_FSM_OUT = "...\sena-PriorityUnion.jsonl"   # required
///   dotnet test --filter "FullyQualifiedName~FsMergeBench"
/// </code>
/// </summary>
[TestFixture]
[Explicit("Manual PR #494 measurement against a local, uncommitted real grammar; not part of CI.")]
public class FsMergeBench
{
    [Test]
    public void MeasureFsMergeMode_OnRealCorpus()
    {
        string? outPath = Environment.GetEnvironmentVariable("HC_FSM_OUT");
        if (string.IsNullOrEmpty(outPath))
            Assert.Ignore("set HC_FSM_OUT to a jsonl output path");

        (Language language, List<string> words) = Load();
        var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);
        int timeoutMs = int.TryParse(Environment.GetEnvironmentVariable("HC_FSM_TIMEOUT_MS"), out int t) ? t : 180000;

        using var writer = new StreamWriter(outPath!, append: false);
        for (int index = 0; index < words.Count; index++)
        {
            string word = words[index];
            AnalysisSyntacticFeatureMerge.ResetCounters();
            var sw = Stopwatch.StartNew();

            List<Word>? analyses = null;
            bool timedOut = false;
            string? error = null;

            // Run on a worker thread so a pathological word times out instead of hanging the whole bench;
            // ParseWord has no cooperative-cancellation hook, so a timed-out word keeps running in the
            // background (same caveat as MemoCorpusVerification.RunWithTimeout). Task.Wait(int) rethrows a
            // faulted task's exception (wrapped in an AggregateException) rather than just returning true,
            // so the exception path has to be a catch here, not an IsFaulted check after the fact.
            Task<List<Word>> task = Task.Run(() => morpher.ParseWord(word).ToList());
            try
            {
                if (!task.Wait(timeoutMs))
                    timedOut = true;
                else
                    analyses = task.Result;
            }
            catch (AggregateException ex)
            {
                error = (ex.InnerException ?? ex).ToString();
            }
            sw.Stop();

            List<string> signature =
                analyses?.Select(AnalysisSignature).OrderBy(s => s, StringComparer.Ordinal).ToList()
                ?? new List<string>();

            var record = new Dictionary<string, object?>
            {
                ["index"] = index,
                ["mode"] = AnalysisSyntacticFeatureMerge.Mode.ToString(),
                ["ms"] = sw.Elapsed.TotalMilliseconds,
                ["timedOut"] = timedOut,
                ["error"] = error,
                ["analysisCount"] = analyses?.Count ?? 0,
                ["signature"] = signature,
                ["checkCalls"] = AnalysisSyntacticFeatureMerge.CheckCalls,
                ["checkRejects"] = AnalysisSyntacticFeatureMerge.CheckRejects,
                ["merges"] = AnalysisSyntacticFeatureMerge.Merges,
                ["exactUnifyFailures"] = AnalysisSyntacticFeatureMerge.ExactUnifyFailures,
                ["analysisAffixApplyCalls"] = AnalysisSyntacticFeatureMerge.AnalysisAffixApplyCalls,
                ["analysisUnapplied"] = AnalysisSyntacticFeatureMerge.AnalysisUnapplied,
                ["lexicalLookupCandidates"] = AnalysisSyntacticFeatureMerge.LexicalLookupCandidates,
                ["synthesisAffixApplyCalls"] = AnalysisSyntacticFeatureMerge.SynthesisAffixApplyCalls,
                ["mergedAnalysesWidened"] = AnalysisSyntacticFeatureMerge.MergedAnalysesWidened,
            };
            writer.WriteLine(JsonSerializer.Serialize(record));
            writer.Flush();

            TestContext.Out.WriteLine(
                $"[{index}] mode={AnalysisSyntacticFeatureMerge.Mode} ms={sw.Elapsed.TotalMilliseconds:F1} "
                    + $"timedOut={timedOut} error={(error != null)} analyses={analyses?.Count ?? 0}"
            );
        }

        Assert.Pass($"wrote {words.Count} record(s) to {outPath}");
    }

    // Two runs of the same word set (same grammar, different AnalysisSyntacticFeatureMerge.Mode) produce
    // identical signatures iff they produced the same set of analyses. Per-analysis: morphemes in surface
    // (morph) order, each with its gloss and the allomorph that realized it (root surface form, or the
    // allomorph's index within its morpheme for a non-root allomorph, since Allomorph.ID is a
    // process-local GUID and cannot be compared across separate dotnet test invocations), plus the
    // analysis' resulting syntactic feature structure -- exactly what AnalysisSyntacticFeatureMerge's three
    // modes can disagree about.
    private static string AnalysisSignature(Word word)
    {
        string morphs = string.Join(
            "+",
            word.AllomorphsInMorphOrder.Select(a => $"{a.Morpheme.Gloss}:{AllomorphKey(a)}")
        );
        return morphs + "|fs=" + word.SyntacticFeatureStruct;
    }

    private static string AllomorphKey(Allomorph allomorph)
    {
        return allomorph is RootAllomorph root ? $"root#{root.Segments.Representation}" : $"idx{allomorph.Index}";
    }

    private static (Language, List<string>) Load()
    {
        string? grammarPath = Environment.GetEnvironmentVariable("HC_FSM_GRAMMAR");
        string? wordsPath = Environment.GetEnvironmentVariable("HC_FSM_WORDS");
        if (string.IsNullOrEmpty(grammarPath) || string.IsNullOrEmpty(wordsPath))
            Assert.Ignore("set HC_FSM_GRAMMAR and HC_FSM_WORDS");

        int maxWords = int.TryParse(Environment.GetEnvironmentVariable("HC_FSM_MAX_WORDS"), out int mw) ? mw : 30;
        Language language = XmlLanguageLoader.Load(grammarPath!);
        List<string> words = File.ReadAllLines(wordsPath!)
            .Select(w => w.Trim())
            .Where(w => w.Length > 0 && !w.StartsWith("#"))
            .Take(maxWords)
            .ToList();
        return (language, words);
    }
}
