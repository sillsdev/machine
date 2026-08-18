using System.Diagnostics;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Stage 5 of memoization.md: the real-data verification the toy-grammar unit tests (stages 2-4)
/// cannot provide. The mrule/template unit tests use 2-3-rule synthetic grammars specifically because
/// they let a specific redundant-order scenario be forced deterministically -- but they cannot exercise
/// whether <see cref="AnalysisStateKey"/>'s key-completeness audit actually holds against the FULL
/// analysis-side rule set a real grammar invokes (<c>AnalysisAffixProcessRule</c>,
/// <c>AnalysisCompoundingRule</c>, <c>AnalysisRealizationalAffixProcessRule</c>, and every phonological
/// rule feeding into the states those rules read). If the key is missing a field some real rule
/// consults, it manifests as a divergence on some real word, nowhere else -- this harness is that check.
///
/// [Explicit] and env-var driven, modeled on FstSenaBenchmark.cs's convention (this repo never commits
/// real morphological grammars or word lists -- see the standing grammar-privacy constraint -- so this
/// test ships with zero embedded grammar/word content and writes no output files, only TestContext
/// lines, to avoid leaking derived corpus data such as signature dumps into a committed path):
///   $env:HC_MEMO_GRAMMAR = "...\sena-hc.xml"
///   $env:HC_MEMO_WORDS   = "...\sena-words.txt"
///   $env:HC_MEMO_MAX_WORDS = "60"          # optional, default 60
///   $env:HC_MEMO_TIMEOUT_MS = "5000"       # optional, default 5000 (per-word watchdog)
///   dotnet test --filter "FullyQualifiedName~MemoCorpusVerification"
///
/// Deliberately NOT the archive's 900-line parallel-benchmark harness (16-way scheduling, GC heap-limit
/// watchdog): per design, the only new runtime configuration this port introduces is single-threaded
/// (<c>maxDegreeOfParallelism: 1</c>), so there is no new concurrency to stress-test here -- just a
/// per-word timeout so one pathological word can't hang the whole run.
/// </summary>
[TestFixture]
[Explicit("Manual corpus verification against a local, uncommitted real grammar; not part of CI.")]
public class MemoCorpusVerification
{
    [Test]
    public void MemoOnMatchesMemoOff_AnalysisSetIdentical_OnRealCorpus()
    {
        (Language language, List<string> words) = Load();

        var memoOff = new Morpher(new TraceManager(), language);
        var memoOn = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);
        int timeoutMs = int.TryParse(Environment.GetEnvironmentVariable("HC_MEMO_TIMEOUT_MS"), out int t) ? t : 5000;

        long mruleHitsBefore = MemoizedCombinationRuleCascade.DiagMemoHits;
        long mruleNogoodsBefore = MemoizedCombinationRuleCascade.DiagNogoodHits;
        long templateHitsBefore = AnalysisStratumRule.DiagTemplateMemoHits;
        long templateNogoodsBefore = AnalysisStratumRule.DiagTemplateNogoodHits;

        var elapsedMsPerWord = new List<double>();
        var perWordTimes = new List<(string Word, double OnMs, double OffMs)>();
        var divergences = new List<string>();
        var timedOut = new List<string>();
        int noParseBoth = 0;

        foreach (string word in words)
        {
            List<string> onSignatures;
            List<string> offSignatures;
            double onMs;
            double offMs;
            try
            {
                var swOn = Stopwatch.StartNew();
                onSignatures = RunWithTimeout(() => Signatures(memoOn, word), timeoutMs);
                swOn.Stop();
                onMs = swOn.Elapsed.TotalMilliseconds;

                var swOff = Stopwatch.StartNew();
                offSignatures = RunWithTimeout(() => Signatures(memoOff, word), timeoutMs);
                swOff.Stop();
                offMs = swOff.Elapsed.TotalMilliseconds;
            }
            catch (TimeoutException)
            {
                timedOut.Add(word);
                continue;
            }
            elapsedMsPerWord.Add(onMs + offMs);
            perWordTimes.Add((word, onMs, offMs));

            if (onSignatures.Count == 0 && offSignatures.Count == 0)
                noParseBoth++;

            if (!onSignatures.SequenceEqual(offSignatures))
            {
                divergences.Add(
                    $"{word}: memo-on={{{string.Join(",", onSignatures)}}} vs "
                        + $"memo-off={{{string.Join(",", offSignatures)}}}"
                );
            }
        }

        elapsedMsPerWord.Sort();
        double p50 = Percentile(elapsedMsPerWord, 0.50);
        double p95 = Percentile(elapsedMsPerWord, 0.95);
        double totalMs = elapsedMsPerWord.Sum();

        TestContext.Out.WriteLine($"words attempted: {words.Count}, timed out (>{timeoutMs}ms): {timedOut.Count}");
        TestContext.Out.WriteLine($"words with no parse on both sides: {noParseBoth}");
        TestContext.Out.WriteLine($"aggregate wall: {totalMs:F1} ms, p50: {p50:F1} ms, p95: {p95:F1} ms");

        // Count-based vs wall-clock aggregates, reported SEPARATELY on purpose: a corpus is typically
        // bimodal (many cheap words the memo makes slightly slower by losing a thread; a few pathological
        // words the memo makes drastically faster) -- collapsing that into one ratio hides which regime a
        // reader is in. "Most words are a bit slower" and "the corpus finishes much faster in total" are
        // both true simultaneously and neither contradicts the other.
        int fasterCount = perWordTimes.Count(x => x.OnMs < x.OffMs);
        int slowerCount = perWordTimes.Count(x => x.OnMs > x.OffMs);
        int tiedCount = perWordTimes.Count - fasterCount - slowerCount;
        double totalOnMs = perWordTimes.Sum(x => x.OnMs);
        double totalOffMs = perWordTimes.Sum(x => x.OffMs);
        TestContext.Out.WriteLine(
            $"count-based: {fasterCount}/{perWordTimes.Count} words faster under memo, "
                + $"{slowerCount}/{perWordTimes.Count} slower, {tiedCount} tied"
        );
        TestContext.Out.WriteLine(
            $"wall-clock: memo-on total {totalOnMs:F1} ms vs memo-off total {totalOffMs:F1} ms "
                + $"({(totalOnMs > 0 ? totalOffMs / totalOnMs : 0):F2}x)"
        );
        if (timedOut.Count > 0)
        {
            // NOT a guaranteed lower bound in either direction: the try block above wraps BOTH the
            // memo-on and memo-off calls, so a timeout could come from either side -- this harness
            // never records which one actually timed out. If it was memo-on, the word's true
            // memo-off time is genuinely unmeasured (could be faster OR slower than the ratio above
            // implies); only a longer timeout actually resolves it. Report the fact, don't imply a
            // direction the data doesn't support.
            TestContext.Out.WriteLine(
                $"(the {timedOut.Count} timed-out word(s) above are excluded from both aggregates; "
                    + "re-run with a higher HC_MEMO_TIMEOUT_MS to actually measure them)"
            );
        }
        // Per-heavy-word attribution (memoization.md's own methodological rule: an aggregate can be
        // dominated by cheap words while hiding what pathological words actually do -- report both).
        // memo-on is sequential+memo; memo-off is the untouched parallel default, so this is the
        // user-visible claim (sequential-memo vs today's shipped behavior), not an isolated measurement
        // of the memo mechanism's own contribution in isolation from single- vs multi-threading.
        TestContext.Out.WriteLine("heaviest words (by memo-off time), memo-on vs memo-off:");
        foreach ((string w, double onMs2, double offMs2) in perWordTimes.OrderByDescending(x => x.OffMs).Take(10))
            TestContext.Out.WriteLine($"  {w}: memo-on {onMs2:F1} ms, memo-off {offMs2:F1} ms");
        TestContext.Out.WriteLine(
            $"mrule memo -- positive hits: {MemoizedCombinationRuleCascade.DiagMemoHits - mruleHitsBefore}, "
                + $"nogood hits: {MemoizedCombinationRuleCascade.DiagNogoodHits - mruleNogoodsBefore}"
        );
        TestContext.Out.WriteLine(
            $"template memo -- positive hits: {AnalysisStratumRule.DiagTemplateMemoHits - templateHitsBefore}, "
                + $"nogood hits: {AnalysisStratumRule.DiagTemplateNogoodHits - templateNogoodsBefore}"
        );
        if (timedOut.Count > 0)
        {
            // Named, not just counted: a timed-out word is EXCLUDED from the equality gate above, so
            // "0 divergences" says nothing about it. These are exactly the candidates for a follow-up
            // run with a longer HC_MEMO_TIMEOUT_MS (see memoization.md §5's addendum on why this
            // mattered -- the heavy words are precisely the ones the memo, and the key-completeness
            // audit, most need to be checked against).
            TestContext.Out.WriteLine(
                $"timed-out words (excluded from the equality gate above -- re-run with a higher "
                    + $"HC_MEMO_TIMEOUT_MS to actually check these): {string.Join(", ", timedOut)}"
            );
        }

        Assert.That(
            divergences,
            Is.Empty,
            $"{divergences.Count} word(s) diverged between memo-on and memo-off "
                + $"(showing up to 10): {string.Join(" | ", divergences.Take(10))}"
        );
        Assert.That(
            MemoizedCombinationRuleCascade.DiagMemoHits + AnalysisStratumRule.DiagTemplateMemoHits,
            Is.GreaterThan(mruleHitsBefore + templateHitsBefore),
            "the positive replay path must actually have fired somewhere in this corpus -- otherwise "
                + "this run cannot distinguish a working memo from a no-op one"
        );
    }

    private static List<string> Signatures(Morpher morpher, string word)
    {
        try
        {
            return morpher
                .ParseWord(word)
                .Select(MorpherTests.WordAnalysisSignature)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
        }
        catch (InvalidShapeException)
        {
            // Matches Morpher.AnalyzeWord's own handling: a word list drawn from real text can contain
            // strings the grammar's character table doesn't cover (e.g. punctuation) -- not a memo
            // concern, both sides would throw identically.
            return new List<string>();
        }
    }

    // Does NOT cancel `action` on timeout -- Morpher.ParseWord has no cooperative-cancellation
    // hook, so a timed-out word's Task keeps running in the background. This can inflate the
    // hit/nogood counters and per-word timings reported below with work from an abandoned prior
    // word, and piled-up orphaned tasks from several timed-out words in a row can starve the
    // thread pool. Acceptable for this [Explicit], never-in-CI diagnostic harness (the equality
    // gate itself is unaffected -- a timed-out word is excluded from it either way, see the
    // caller), but do not read the reported hit counts/timings as precise when timeouts occurred.
    private static T RunWithTimeout<T>(Func<T> action, int timeoutMs)
    {
        Task<T> task = Task.Run(action);
        if (!task.Wait(timeoutMs))
            throw new TimeoutException();
        return task.Result;
    }

    private static double Percentile(List<double> sortedValues, double fraction)
    {
        if (sortedValues.Count == 0)
            return 0;
        int index = (int)Math.Ceiling(fraction * sortedValues.Count) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Count - 1)];
    }

    private static (Language, List<string>) Load()
    {
        string? grammarPath = Environment.GetEnvironmentVariable("HC_MEMO_GRAMMAR");
        string? wordsPath = Environment.GetEnvironmentVariable("HC_MEMO_WORDS");
        if (string.IsNullOrEmpty(grammarPath) || string.IsNullOrEmpty(wordsPath))
            Assert.Ignore("set HC_MEMO_GRAMMAR and HC_MEMO_WORDS");

        int maxWords = int.TryParse(Environment.GetEnvironmentVariable("HC_MEMO_MAX_WORDS"), out int mw) ? mw : 60;
        Language language = XmlLanguageLoader.Load(grammarPath!);
        List<string> words = File.ReadAllLines(wordsPath!)
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .Take(maxWords)
            .ToList();
        return (language, words);
    }
}
