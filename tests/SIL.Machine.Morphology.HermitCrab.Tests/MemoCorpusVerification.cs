using System.Diagnostics;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Memo-on/memo-off equality against a real grammar, which is the only way to test
/// <see cref="AnalysisStateKey"/>'s key-completeness audit against the full analysis-side rule set. The
/// synthetic unit-test grammars can force a specific redundant order deterministically but cannot reach
/// that breadth; a key missing a field some real rule reads shows up here and nowhere else.
/// <para>
/// [Explicit] and env-var driven because this repo never commits real grammars or word lists: the test
/// embeds no grammar content and writes only TestContext lines, so no derived corpus data (signature
/// dumps included) can land in a committed path.
/// </para>
/// <code>
///   $env:HC_MEMO_GRAMMAR = "...\sena-hc.xml"
///   $env:HC_MEMO_WORDS   = "...\sena-words.txt"
///   $env:HC_MEMO_MAX_WORDS = "60"          # optional, default 60
///   $env:HC_MEMO_TIMEOUT_MS = "5000"       # optional, default 5000 (per-word watchdog)
///   dotnet test --filter "FullyQualifiedName~MemoCorpusVerification"
/// </code>
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

        // Count-based and wall-clock aggregates stay separate because a corpus is bimodal: many cheap
        // words go slightly slower for want of a thread, while a few pathological ones go far faster. One
        // combined ratio would hide which regime a reader is in, and both statements are true at once.
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
            // The ratio above is not a bound in either direction: one try block wraps both calls, so which
            // side timed out is unrecorded, and if it was memo-on then that word's memo-off time was never
            // measured at all.
            TestContext.Out.WriteLine(
                $"(the {timedOut.Count} timed-out word(s) above are excluded from both aggregates; "
                    + "re-run with a higher HC_MEMO_TIMEOUT_MS to actually measure them)"
            );
        }
        // Per-word attribution as well, since an aggregate dominated by cheap words hides what the
        // pathological ones do. Note memo-on is sequential while memo-off is the parallel default, so
        // these times measure the user-visible comparison, not the memo's contribution in isolation.
        TestContext.Out.WriteLine("heaviest words (by memo-off time), memo-on vs memo-off:");
        foreach ((string w, double onMs2, double offMs2) in perWordTimes.OrderByDescending(x => x.OffMs).Take(10))
            TestContext.Out.WriteLine($"  {w}: memo-on {onMs2:F1} ms, memo-off {offMs2:F1} ms");
        TestContext.Out.WriteLine($"mrule memo -- positive hits: {memoOn.MemoHits}, nogood hits: {memoOn.NogoodHits}");
        TestContext.Out.WriteLine(
            $"template memo -- positive hits: {memoOn.TemplateMemoHits}, nogood hits: {memoOn.TemplateNogoodHits}"
        );
        if (timedOut.Count > 0)
        {
            // Named rather than counted: these words are excluded from the equality gate, so "0
            // divergences" says nothing about them, and heavy words are exactly what the memo and the
            // key-completeness audit most need checking against.
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
            memoOn.MemoHits + memoOn.TemplateMemoHits,
            Is.GreaterThan(0),
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
            // As Morpher.AnalyzeWord does: a real word list can contain strings the character table does
            // not cover, which both sides reject identically and which tells us nothing about the memo.
            return new List<string>();
        }
    }

    // Cannot cancel `action`: ParseWord has no cooperative-cancellation hook, so a timed-out word keeps
    // running in the background, where it can inflate later words' counters and timings, and enough
    // orphaned tasks in a row can starve the thread pool. Tolerable only because this harness never runs
    // in CI, and the equality gate excludes timed-out words anyway -- but treat any run that reported
    // timeouts as having approximate counts.
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
