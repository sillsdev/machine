#nullable disable
using System.Diagnostics;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Memo-on/memo-off equality for <see cref="Morpher.UseSynthesisFoldMemo"/>
/// (docs/hermitcrab-synthesis-fold-probes.md), the synthesis-side counterpart to
/// <see cref="MemoCorpusVerification"/>. Both sides run at <c>maxDegreeOfParallelism: 1</c> -- the toggle
/// only takes effect there -- so the only variable is the toggle itself.
/// <para>
/// Two gates: the 33 committed conformance fixtures (fast, synthetic, safe to run unconditionally) and a
/// real corpus via the same env vars <see cref="MemoCorpusVerification"/> uses (this repo never commits
/// real grammars or word lists). [Explicit] throughout, matching every other corpus/fixture-sweep test in
/// this file's neighbourhood (<see cref="SynthesisFoldProbe"/>, <see cref="MemoCorpusVerification"/>): not
/// part of CI, run manually.
/// </para>
/// <code>
///   dotnet test --filter "FullyQualifiedName~SynthesisFoldMemoVerification.MemoOnMatchesMemoOff_AnalysisSetIdentical_AcrossConformanceFixtures"
///
///   $env:HC_MEMO_GRAMMAR = "...\sena-hc.xml"
///   $env:HC_MEMO_WORDS   = "...\sena-words.txt"
///   $env:HC_MEMO_MAX_WORDS = "60"
///   $env:HC_MEMO_TIMEOUT_MS = "600000"
///   dotnet test --filter "FullyQualifiedName~SynthesisFoldMemoVerification.MemoOnMatchesMemoOff_AnalysisSetIdentical_OnRealCorpus"
/// </code>
/// </summary>
[TestFixture]
[Explicit("Manual corpus/fixture verification; not part of CI. See docs/hermitcrab-synthesis-fold-probes.md.")]
public class SynthesisFoldMemoVerification
{
    [Test]
    public void MemoOnMatchesMemoOff_AnalysisSetIdentical_AcrossConformanceFixtures()
    {
        string fixturesRoot = Environment.GetEnvironmentVariable("HC_PROBE_FIXTURES_ROOT");
        if (string.IsNullOrEmpty(fixturesRoot))
            fixturesRoot = Path.Combine(RepositoryRoot(), "conformance");

        List<Fixture> fixtures = Fixture.DiscoverAll(fixturesRoot);
        Assert.That(fixtures, Is.Not.Empty, $"no fixtures discovered under {fixturesRoot}");

        var divergences = new List<string>();
        long totalHitsAcrossFixtures = 0;
        var fixtureTimings = new List<(string Id, double OnMs, double OffMs, int Words)>();

        foreach (Fixture fixture in fixtures)
        {
            Language language;
            try
            {
                language = XmlLanguageLoader.Load(fixture.GrammarPath);
            }
            catch (Exception e)
            {
                TestContext.Out.WriteLine($"[{fixture.Id}] grammar failed to load: {e.GetType().Name} -- skipped");
                continue;
            }

            var memoOff = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);
            var memoOn = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1)
            {
                UseSynthesisFoldMemo = true,
            };

            double onMs = 0;
            double offMs = 0;
            foreach (WordEntry entry in fixture.Words.Words)
            {
                var swOff = Stopwatch.StartNew();
                List<string> offSignatures = Signatures(memoOff, entry.Word);
                swOff.Stop();
                offMs += swOff.Elapsed.TotalMilliseconds;

                var swOn = Stopwatch.StartNew();
                List<string> onSignatures = Signatures(memoOn, entry.Word);
                swOn.Stop();
                onMs += swOn.Elapsed.TotalMilliseconds;

                if (!onSignatures.SequenceEqual(offSignatures))
                {
                    divergences.Add(
                        $"[{fixture.Id}] {entry.Word}: memo-on={{{string.Join(",", onSignatures)}}} vs "
                            + $"memo-off={{{string.Join(",", offSignatures)}}}"
                    );
                }
            }

            totalHitsAcrossFixtures += memoOn.SynthesisFoldHits;
            fixtureTimings.Add((fixture.Id, onMs, offMs, fixture.Words.Words.Count));
            TestContext.Out.WriteLine(
                $"[{fixture.Id}] words={fixture.Words.Words.Count} memo-on={onMs:F2}ms memo-off={offMs:F2}ms "
                    + $"hits={memoOn.SynthesisFoldHits} "
                    + $"speedup={(onMs > 0 ? offMs / onMs : 0):F2}x"
            );
        }

        TestContext.Out.WriteLine($"total synthesis fold-memo hits across all fixtures: {totalHitsAcrossFixtures}");
        TestContext.Out.WriteLine("--- the two reliable fixtures from section 8 of the plan doc ---");
        foreach (
            string id in new[]
            {
                "edge-cases/deep-optional-affix-nesting",
                "languages/suffixing-evidential-adjacency-chain",
            }
        )
        {
            var row = fixtureTimings.FirstOrDefault(r => r.Id == id);
            if (row.Id == null)
            {
                TestContext.Out.WriteLine($"{id}: not found among discovered fixtures");
                continue;
            }
            double speedup = row.OnMs > 0 ? row.OffMs / row.OnMs : 0;
            double wallSaved = row.OffMs > 0 ? (1 - row.OnMs / row.OffMs) * 100 : 0;
            TestContext.Out.WriteLine(
                $"{id}: memo-off={row.OffMs:F2}ms memo-on={row.OnMs:F2}ms speedup={speedup:F2}x "
                    + $"wall-clock-reduction={wallSaved:F1}%"
            );
        }

        Assert.That(
            divergences,
            Is.Empty,
            $"{divergences.Count} word(s) diverged between memo-on and memo-off across the 33 conformance "
                + $"fixtures (showing up to 10): {string.Join(" | ", divergences.Take(10))}"
        );
    }

    // One discarded warm-up per arm per word, then MeasuredReps interleaved samples of which the MINIMUM
    // is kept, on FRESH Morphers built just for this test -- the correctness sweep above reuses one
    // Morpher per fixture across every word, which is fine for equality but leaves memo tables/JIT state
    // that would bias a single-pass timing comparison. Interleaving off/on per rep, rather than running
    // all off reps then all on reps, means a one-off GC or JIT stall lands on both arms rather than
    // whichever ran first. section 6.1 of the plan doc found sub-2ms fixtures unreliable to time at all;
    // this is the section-8-style rigor those two numbers specifically need.
    private const int WarmupReps = 1;
    private const int MeasuredReps = 5;

    [Test]
    public void MeasuredSpeedup_OnTheTwoReliableFixtures()
    {
        string fixturesRoot = Environment.GetEnvironmentVariable("HC_PROBE_FIXTURES_ROOT");
        if (string.IsNullOrEmpty(fixturesRoot))
            fixturesRoot = Path.Combine(RepositoryRoot(), "conformance");
        List<Fixture> fixtures = Fixture.DiscoverAll(fixturesRoot);

        foreach (
            string id in new[]
            {
                "edge-cases/deep-optional-affix-nesting",
                "languages/suffixing-evidential-adjacency-chain",
            }
        )
        {
            Fixture fixture = fixtures.FirstOrDefault(f => f.Id == id);
            if (fixture == null)
            {
                TestContext.Out.WriteLine($"{id}: not found among discovered fixtures");
                continue;
            }

            Language language = XmlLanguageLoader.Load(fixture.GrammarPath);
            var off = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);
            var on = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1)
            {
                UseSynthesisFoldMemo = true,
            };

            for (int w = 0; w < WarmupReps; w++)
            {
                foreach (WordEntry entry in fixture.Words.Words)
                {
                    Signatures(off, entry.Word);
                    Signatures(on, entry.Word);
                }
            }

            double offMin = double.MaxValue;
            double offMax = 0;
            double onMin = double.MaxValue;
            for (int r = 0; r < MeasuredReps; r++)
            {
                var swOff = Stopwatch.StartNew();
                foreach (WordEntry entry in fixture.Words.Words)
                    Signatures(off, entry.Word);
                swOff.Stop();

                var swOn = Stopwatch.StartNew();
                foreach (WordEntry entry in fixture.Words.Words)
                    Signatures(on, entry.Word);
                swOn.Stop();

                double offMs = swOff.Elapsed.TotalMilliseconds;
                double onMs = swOn.Elapsed.TotalMilliseconds;
                offMin = Math.Min(offMin, offMs);
                offMax = Math.Max(offMax, offMs);
                onMin = Math.Min(onMin, onMs);
            }

            double speedup = onMin > 0 ? offMin / onMin : 0;
            double wallSaved = offMin > 0 ? (1 - onMin / offMin) * 100 : 0;
            // Off-arm (max - min) as a percentage of the off-arm floor: how much of any apparent delta
            // could just be noise. A speedup implying less wall-clock reduction than this figure is not a
            // result.
            double offSpreadPct = offMin > 0 ? (offMax - offMin) / offMin * 100 : 0;
            TestContext.Out.WriteLine(
                $"{id}: memo-off(min of {MeasuredReps})={offMin:F2}ms memo-on(min of {MeasuredReps})={onMin:F2}ms "
                    + $"speedup={speedup:F2}x wall-clock-reduction={wallSaved:F1}% "
                    + $"off-arm-spread={offSpreadPct:F1}% hits={on.SynthesisFoldHits}"
            );
        }
    }

    [Test]
    public void MemoOnMatchesMemoOff_AnalysisSetIdentical_OnRealCorpus()
    {
        (Language language, List<string> words) = Load();

        var memoOff = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);
        var memoOn = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1)
        {
            UseSynthesisFoldMemo = true,
        };
        int timeoutMs = int.TryParse(Environment.GetEnvironmentVariable("HC_MEMO_TIMEOUT_MS"), out int t)
            ? t
            : 5000;

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
                var swOff = Stopwatch.StartNew();
                offSignatures = RunWithTimeout(() => Signatures(memoOff, word), timeoutMs);
                swOff.Stop();
                offMs = swOff.Elapsed.TotalMilliseconds;

                var swOn = Stopwatch.StartNew();
                onSignatures = RunWithTimeout(() => Signatures(memoOn, word), timeoutMs);
                swOn.Stop();
                onMs = swOn.Elapsed.TotalMilliseconds;
            }
            catch (TimeoutException)
            {
                timedOut.Add(word);
                continue;
            }
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

            TestContext.Out.WriteLine(
                $"{word}: memo-off={offMs:F1}ms memo-on={onMs:F1}ms "
                    + $"speedup={(onMs > 0 ? offMs / onMs : 0):F2}x hits-so-far={memoOn.SynthesisFoldHits}"
            );
        }

        double totalOnMs = perWordTimes.Sum(x => x.OnMs);
        double totalOffMs = perWordTimes.Sum(x => x.OffMs);
        TestContext.Out.WriteLine($"words attempted: {words.Count}, timed out (>{timeoutMs}ms): {timedOut.Count}");
        TestContext.Out.WriteLine($"words with no parse on both sides: {noParseBoth}");
        TestContext.Out.WriteLine(
            $"wall-clock: memo-on total {totalOnMs:F1} ms vs memo-off total {totalOffMs:F1} ms "
                + $"({(totalOnMs > 0 ? totalOffMs / totalOnMs : 0):F2}x)"
        );
        TestContext.Out.WriteLine($"synthesis fold-memo hits (final Morpher totals): {memoOn.SynthesisFoldHits}");
        if (timedOut.Count > 0)
        {
            TestContext.Out.WriteLine(
                $"timed-out words (excluded from the equality gate above): {string.Join(", ", timedOut)}"
            );
        }

        Assert.That(
            divergences,
            Is.Empty,
            $"{divergences.Count} word(s) diverged between memo-on and memo-off "
                + $"(showing up to 10): {string.Join(" | ", divergences.Take(10))}"
        );
    }

    // Some conformance fixtures (e.g. edge-cases/simultaneous-epenthesis-cascade) are deliberately built
    // so that a CORRECT C# engine throws -- InvalidShapeException for an out-of-inventory character (as
    // Morpher.AnalyzeWord already tolerates) and InfiniteLoopException for a rewrite-rule runaway the
    // engine's own hard cap catches. Neither is a memo concern: both are thrown well outside the
    // memoized region (character-table lookup and post-fold phonological rewriting respectively). Rather
    // than special-case fixture ids, any such crash is folded into a one-element sentinel signature so
    // memo-on/memo-off comparison still works uniformly -- if only one side crashes, or the two sides
    // crash with different exception types, that sentinel mismatch surfaces as a real divergence.
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
            return new List<string>();
        }
        catch (Exception e) when (e is InfiniteLoopException or global::SIL.Machine.Rules.MaxAlternativesExceededException)
        {
            return new List<string> { $"<<CRASH:{e.GetType().Name}>>" };
        }
    }

    // See MemoCorpusVerification.RunWithTimeout for why this cannot cooperatively cancel.
    private static T RunWithTimeout<T>(Func<T> action, int timeoutMs)
    {
        Task<T> task = Task.Run(action);
        if (!task.Wait(timeoutMs))
            throw new TimeoutException();
        return task.Result;
    }

    private static string RepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    private static (Language, List<string>) Load()
    {
        string grammarPath = Environment.GetEnvironmentVariable("HC_MEMO_GRAMMAR");
        string wordsPath = Environment.GetEnvironmentVariable("HC_MEMO_WORDS");
        if (string.IsNullOrEmpty(grammarPath) || string.IsNullOrEmpty(wordsPath))
            Assert.Ignore("set HC_MEMO_GRAMMAR and HC_MEMO_WORDS");

        int maxWords = int.TryParse(Environment.GetEnvironmentVariable("HC_MEMO_MAX_WORDS"), out int mw) ? mw : 60;
        Language language = XmlLanguageLoader.Load(grammarPath);
        List<string> words = File.ReadAllLines(wordsPath)
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .Take(maxWords)
            .ToList();
        return (language, words);
    }
}
