#nullable disable
using System.Diagnostics;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// P1 harness for docs/hermitcrab-synthesis-fold-probes.md section 3: one pass over
/// <see cref="Morpher"/>'s synthesis loop (via <see cref="SynthesisProbe"/>) that emits all three P1
/// measurements per word -- the wall-time split (P1a), the die-point histogram (P1b), and the
/// fold-step fingerprint ratio (P1c) -- plus the determinism check P1c depends on.
/// <para>
/// Always runs the memoized path (<c>maxDegreeOfParallelism: 1</c>): that is the only sequential
/// cascade, and P1c's premise is entirely about what the memo already identifies as shared.
/// </para>
/// <para>
/// [Explicit] and env-var driven for the real-corpus mode, matching <see cref="MemoCorpusVerification"/>:
/// this repo never commits real grammars or word lists, so the test embeds no grammar content and
/// writes only TestContext lines. The conformance-fixture mode needs no env vars -- the 33 fixtures
/// under conformance/ are synthetic and committed, so their ids and per-word counts are safe to print.
/// Real-corpus output stays to counts, ratios, timings, and die-point categories only -- never rule
/// names, stratum names, morpheme glosses, or lexical entries.
/// </para>
/// <code>
///   # breadth: all 33 conformance fixtures, no env vars needed
///   dotnet test --filter "FullyQualifiedName~SynthesisFoldProbe.Probe_ConformanceFixtures"
///
///   # depth: a real corpus
///   $env:HC_MEMO_GRAMMAR = "...\sena-hc.xml"
///   $env:HC_MEMO_WORDS   = "...\sena-words.txt"      # optional if HC_PROBE_WORDS is set
///   $env:HC_MEMO_MAX_WORDS = "60"                    # optional, default 60
///   $env:HC_PROBE_WORDS = "atawirambo,cinacemerwa"   # optional explicit override, comma-separated
///   dotnet test --filter "FullyQualifiedName~SynthesisFoldProbe.Probe_RealCorpus"
/// </code>
/// </summary>
[TestFixture]
[Explicit("Manual instrumentation run; not part of CI. See docs/hermitcrab-synthesis-fold-probes.md.")]
public class SynthesisFoldProbe
{
    private static readonly SynthesisDiePoint[] AllDiePoints = (SynthesisDiePoint[])
        Enum.GetValues(typeof(SynthesisDiePoint));

    [Test]
    public void Probe_ConformanceFixtures()
    {
        string fixturesRoot = Environment.GetEnvironmentVariable("HC_PROBE_FIXTURES_ROOT");
        if (string.IsNullOrEmpty(fixturesRoot))
            fixturesRoot = Path.Combine(RepositoryRoot(), "conformance");

        List<Fixture> fixtures = Fixture.DiscoverAll(fixturesRoot);
        Assert.That(fixtures, Is.Not.Empty, $"no fixtures discovered under {fixturesRoot}");

        SynthesisProbe.Enabled = true;
        long grandDeterminismViolations = 0;
        var fixtureRatios =
            new List<(string Id, double Ratio, long Applications, long Distinct, double ForwardShare, double Value)>();
        try
        {
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
                var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);
                SynthesisProbe.ResetFoldSteps();

                var rows = new List<WordProbeResult>();
                foreach (WordEntry entry in fixture.Words.Words)
                {
                    WordProbeResult row = ProbeWord(morpher, entry.Word);
                    if (row != null)
                        rows.Add(row);
                }

                PrintFixtureSummary(fixture.Id, rows);
                long applications = SynthesisProbe.TotalApplications;
                long distinct = SynthesisProbe.DistinctFoldSteps;
                double ratio = distinct > 0 ? applications / (double)distinct : 0;
                // Forward-synthesis share of wall time for this fixture (pooled across its words), and the
                // "value" of P1c's fold-sharing ratio for this grammar: sharing that never reaches forward
                // synthesis cannot be realized as a speedup by folding forward-synthesis steps, so ratio
                // alone overstates the payoff on a grammar where forward synthesis is a small slice of wall
                // time. See the scope-change note in the P1a follow-up: this is per-fixture, not pooled,
                // because the payoff is grammar-specific.
                double fixtureWall = rows.Sum(r => r.WallMs);
                double fixtureForward = rows.Sum(r => r.SynForwardMs);
                double forwardShare = fixtureWall > 0 ? fixtureForward / fixtureWall : 0;
                double value = ratio * forwardShare;
                fixtureRatios.Add((fixture.Id, ratio, applications, distinct, forwardShare, value));
                grandDeterminismViolations += SynthesisProbe.DeterminismViolations;
            }

            TestContext.Out.WriteLine();
            TestContext.Out.WriteLine(
                "=== P1c ratio by fixture (not pooled -- fixtures vary wildly in size); "
                    + "value = ratio x forward-synthesis share of wall time ==="
            );
            foreach (
                (string id, double ratio, long applications, long distinct, double forwardShare, double value)
                    in fixtureRatios
            )
            {
                TestContext.Out.WriteLine(
                    $"  {id}\tapplications={applications}\tdistinct={distinct}\tratio={ratio:F2}x\t"
                        + $"forwardShare={forwardShare * 100:F1}%\tvalue={value:F2}"
                );
            }
            TestContext.Out.WriteLine();
            TestContext.Out.WriteLine(
                $"=== DETERMINISM VIOLATIONS across all fixtures: {grandDeterminismViolations} "
                    + $"{(grandDeterminismViolations > 0 ? "(fingerprint is INCOMPLETE -- see class remarks)" : "(none observed)")} ==="
            );
        }
        finally
        {
            SynthesisProbe.Enabled = false;
            SynthesisProbe.ResetAll();
        }
    }

    [Test]
    public void Probe_RealCorpus()
    {
        (Language language, List<string> words) = LoadRealCorpus();

        var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);
        SynthesisProbe.Enabled = true;
        SynthesisProbe.ResetFoldSteps();
        try
        {
            var rows = new List<WordProbeResult>();
            foreach (string word in words)
            {
                WordProbeResult row = ProbeWord(morpher, word);
                if (row != null)
                    rows.Add(row);
            }

            PrintFixtureSummary("real-corpus", rows);
            long applications = SynthesisProbe.TotalApplications;
            long distinct = SynthesisProbe.DistinctFoldSteps;
            double ratio = distinct > 0 ? applications / (double)distinct : 0;
            long violations = SynthesisProbe.DeterminismViolations;
            TestContext.Out.WriteLine();
            TestContext.Out.WriteLine(
                $"=== P1c: applications={applications}, distinct={distinct}, ratio={ratio:F2}x ==="
            );
            TestContext.Out.WriteLine(
                $"=== DETERMINISM VIOLATIONS: {violations} "
                    + $"{(violations > 0 ? "(fingerprint is INCOMPLETE -- see SynthesisProbe class remarks)" : "(none observed)")} ==="
            );
        }
        finally
        {
            SynthesisProbe.Enabled = false;
            SynthesisProbe.ResetAll();
        }
    }

    private sealed class WordProbeResult
    {
        public string Word;
        public int ParseCount;
        public double WallMs;
        public double LexicalLookupMs;
        public double SynCascadeMs;
        public double SynBatteryMs;
        public double SynForwardMs;
        public double AnTotalMs;
        public double AnCascadeMs;
        public double AnBatteryMs;
        public double AnPhonoMs;
        public double UnaccountedMs;
        public long[] DieCounts;
        public long ApplicationsThisWord;
        public long NewDistinctThisWord;

        // Forward-synthesis share of wall time -- the multiplier the P1c sharing ratio needs to turn into
        // an actual expected win (P1c ratio x this share; see the "value" column in PrintFixtureSummary).
        // Sharing that never reaches forward synthesis (the cascade/battery/lookup buckets, or analysis
        // time) cannot be realized by folding forward-synthesis steps.
        public double SynForwardShare => WallMs > 0 ? SynForwardMs / WallMs : 0;
    }

    /// <summary>
    /// Runs one word through the instrumented sequential path and returns its P1a/P1b/P1c deltas.
    /// P1a/P1b timers and counters are reset immediately before the word (so the result is a per-word
    /// delta); the P1c fold-step table is NOT reset here -- it accumulates across a whole fixture/corpus
    /// so the distinct-pair count is a true count over the combined stream, and this method instead
    /// snapshots <see cref="SynthesisProbe.TotalApplications"/>/<see cref="SynthesisProbe.DistinctFoldSteps"/>
    /// before and after to report this word's own contribution.
    /// </summary>
    private static WordProbeResult ProbeWord(Morpher morpher, string word)
    {
        SynthesisProbe.ResetWallTime();
        SynthesisProbe.ResetDiePoints();
        long applicationsBefore = SynthesisProbe.TotalApplications;
        long distinctBefore = SynthesisProbe.DistinctFoldSteps;

        int parseCount;
        var wall = Stopwatch.StartNew();
        try
        {
            parseCount = morpher.ParseWord(word).Count();
        }
        catch (InvalidShapeException)
        {
            // As Morpher.AnalyzeWord/MemoCorpusVerification do: a word list can contain strings the
            // character table does not cover. Both memo-on and memo-off reject these identically and no
            // synthesis is attempted, so there is nothing for this probe to measure.
            return null;
        }
        catch (Exception e)
        {
            // A handful of edge-case fixtures have ExpectCrash: true -- their ground truth IS a thrown
            // exception (e.g. an epenthesis rule hitting the infinite-loop guard), not a signature. This
            // probe is not the self-check for that contract (Fixture's own self-check already covers it),
            // so it just notes the crash and skips measurement for this word rather than aborting the
            // whole fixture/run. The exception type name is a .NET framework/engine identifier, not
            // grammar content, so it is safe to print.
            TestContext.Out.WriteLine($"  {word}\tCRASHED: {e.GetType().Name}");
            return null;
        }
        wall.Stop();

        var dieCounts = new long[AllDiePoints.Length];
        for (int i = 0; i < AllDiePoints.Length; i++)
            dieCounts[i] = SynthesisProbe.GetDieCount(AllDiePoints[i]);

        double wallMs = wall.Elapsed.TotalMilliseconds;
        double lookupMs = TicksToMs(SynthesisProbe.LexicalLookupTicks);
        double synCascadeMs = TicksToMs(SynthesisProbe.SynCascadeTicks);
        double synBatteryMs = TicksToMs(SynthesisProbe.SynBatteryTicks);
        double synForwardMs = TicksToMs(SynthesisProbe.SynForwardTicks);
        double anTotalMs = TicksToMs(SynthesisProbe.AnTotalTicks);
        double anCascadeMs = TicksToMs(SynthesisProbe.AnCascadeTicks);
        double anBatteryMs = TicksToMs(SynthesisProbe.AnBatteryTicks);
        double anPhonoMs = TicksToMs(SynthesisProbe.AnPhonoTicks);

        // Top-level buckets are disjoint by construction (see SynthesisProbe's wall-time-split remarks):
        // AnTotalMs is the whole analysis phase (a nested/inclusive total that already contains
        // AnCascade/AnBattery/AnPhono), and the syn*/lookup buckets are disjoint slices of the synthesis
        // phase (SynForwardMs is already net of SynCascade/SynBattery, see Morpher.SynthesizeSequential).
        // So unaccounted = wall - analysis phase - synthesis phase, i.e. ParseWord's own scaffolding
        // (shape segmentation, Word construction/Freeze, AccumulateMemoDiagnostics, guessRoot) plus any
        // region this probe does not yet bracket.
        double unaccountedMs = wallMs - anTotalMs - lookupMs - synCascadeMs - synBatteryMs - synForwardMs;

        return new WordProbeResult
        {
            Word = word,
            ParseCount = parseCount,
            WallMs = wallMs,
            LexicalLookupMs = lookupMs,
            SynCascadeMs = synCascadeMs,
            SynBatteryMs = synBatteryMs,
            SynForwardMs = synForwardMs,
            AnTotalMs = anTotalMs,
            AnCascadeMs = anCascadeMs,
            AnBatteryMs = anBatteryMs,
            AnPhonoMs = anPhonoMs,
            UnaccountedMs = unaccountedMs,
            DieCounts = dieCounts,
            ApplicationsThisWord = SynthesisProbe.TotalApplications - applicationsBefore,
            NewDistinctThisWord = SynthesisProbe.DistinctFoldSteps - distinctBefore,
        };
    }

    private static double TicksToMs(long stopwatchTicks) => stopwatchTicks * 1000.0 / Stopwatch.Frequency;

    private static void PrintFixtureSummary(string fixtureId, List<WordProbeResult> rows)
    {
        TestContext.Out.WriteLine();
        TestContext.Out.WriteLine($"--- {fixtureId} ({rows.Count} word(s) measured) ---");
        // Nesting scheme (P1a follow-up, docs/hermitcrab-synthesis-fold-probes.md section 3): anTotal is a
        // NESTED/INCLUSIVE total for the whole analysis phase (it contains anCascade+anBattery+anPhono, so
        // those three do not add on top of it). Every other column here -- lookup, synCascade, synBattery,
        // synForward, anTotal, unaccounted -- is an EXCLUSIVE slice of wall time; those six sum to wall
        // exactly (unaccounted is defined as the remainder). "apps+"/"newDistinct+" are P1c counters, not
        // wall-time buckets.
        foreach (WordProbeResult r in rows)
        {
            TestContext.Out.WriteLine(
                $"  {r.Word}\tparses={r.ParseCount}\twall={r.WallMs:F2}ms\t"
                    + $"lookup={r.LexicalLookupMs:F2}\tsynCascade={r.SynCascadeMs:F2}\t"
                    + $"synBattery={r.SynBatteryMs:F2}\tsynForward={r.SynForwardMs:F2}\t"
                    + $"anTotal={r.AnTotalMs:F2} [anCascade={r.AnCascadeMs:F2} anBattery={r.AnBatteryMs:F2} anPhono={r.AnPhonoMs:F2}]\t"
                    + $"unaccounted={r.UnaccountedMs:F2}\t"
                    + $"apps+={r.ApplicationsThisWord}\tnewDistinct+={r.NewDistinctThisWord}"
            );
        }

        if (rows.Count == 0)
        {
            TestContext.Out.WriteLine("  (no words measured)");
            return;
        }

        double sumWall = rows.Sum(r => r.WallMs);
        double sumLookup = rows.Sum(r => r.LexicalLookupMs);
        double sumSynCascade = rows.Sum(r => r.SynCascadeMs);
        double sumSynBattery = rows.Sum(r => r.SynBatteryMs);
        double sumSynForward = rows.Sum(r => r.SynForwardMs);
        double sumAnTotal = rows.Sum(r => r.AnTotalMs);
        double sumAnCascade = rows.Sum(r => r.AnCascadeMs);
        double sumAnBattery = rows.Sum(r => r.AnBatteryMs);
        double sumAnPhono = rows.Sum(r => r.AnPhonoMs);
        double sumUnaccounted = rows.Sum(r => r.UnaccountedMs);
        TestContext.Out.WriteLine(
            $"  [P1a totals -- exclusive slices, sum to wall] wall={sumWall:F2}ms "
                + $"lookup={sumLookup:F2}ms ({Pct(sumLookup, sumWall)}) "
                + $"synCascade={sumSynCascade:F2}ms ({Pct(sumSynCascade, sumWall)}) "
                + $"synBattery={sumSynBattery:F2}ms ({Pct(sumSynBattery, sumWall)}) "
                + $"synForward={sumSynForward:F2}ms ({Pct(sumSynForward, sumWall)}) "
                + $"anTotal={sumAnTotal:F2}ms ({Pct(sumAnTotal, sumWall)}) "
                + $"unaccounted={sumUnaccounted:F2}ms ({Pct(sumUnaccounted, sumWall)})"
        );
        TestContext.Out.WriteLine(
            $"  [anTotal breakdown -- nested inside anTotal, not on top of it] "
                + $"anCascade={sumAnCascade:F2}ms ({Pct(sumAnCascade, sumAnTotal)} of anTotal) "
                + $"anBattery={sumAnBattery:F2}ms ({Pct(sumAnBattery, sumAnTotal)} of anTotal) "
                + $"anPhono={sumAnPhono:F2}ms ({Pct(sumAnPhono, sumAnTotal)} of anTotal) "
                + $"anOther={sumAnTotal - sumAnCascade - sumAnBattery - sumAnPhono:F2}ms "
                + $"({Pct(sumAnTotal - sumAnCascade - sumAnBattery - sumAnPhono, sumAnTotal)} of anTotal)"
        );

        var dieTotals = new long[AllDiePoints.Length];
        foreach (WordProbeResult r in rows)
        {
            for (int i = 0; i < AllDiePoints.Length; i++)
                dieTotals[i] += r.DieCounts[i];
        }
        long dieGrandTotal = dieTotals.Sum();
        TestContext.Out.Write("  [P1b die points] ");
        if (dieGrandTotal == 0)
        {
            TestContext.Out.WriteLine("no rejections recorded");
        }
        else
        {
            TestContext.Out.WriteLine(
                string.Join(
                    "  ",
                    AllDiePoints.Select(
                        (p, i) => $"{p}={dieTotals[i]} ({Pct(dieTotals[i], dieGrandTotal)})"
                    )
                )
            );
        }

        long applications = SynthesisProbe.TotalApplications;
        long distinct = SynthesisProbe.DistinctFoldSteps;
        double ratio = distinct > 0 ? applications / (double)distinct : 0;
        TestContext.Out.WriteLine(
            $"  [P1c] applications={applications} distinct={distinct} ratio={ratio:F2}x "
                + $"(cumulative for this fixture/corpus so far)"
        );
    }

    private static string Pct(double part, double whole) => whole > 0 ? $"{part / whole * 100:F1}%" : "n/a";

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

    private static (Language, List<string>) LoadRealCorpus()
    {
        string grammarPath = Environment.GetEnvironmentVariable("HC_MEMO_GRAMMAR");
        if (string.IsNullOrEmpty(grammarPath))
            Assert.Ignore("set HC_MEMO_GRAMMAR (and either HC_PROBE_WORDS or HC_MEMO_WORDS)");

        Language language = XmlLanguageLoader.Load(grammarPath!);

        string explicitWords = Environment.GetEnvironmentVariable("HC_PROBE_WORDS");
        if (!string.IsNullOrEmpty(explicitWords))
        {
            List<string> words = explicitWords!
                .Split(',')
                .Select(w => w.Trim())
                .Where(w => w.Length > 0)
                .ToList();
            return (language, words);
        }

        string wordsPath = Environment.GetEnvironmentVariable("HC_MEMO_WORDS");
        if (string.IsNullOrEmpty(wordsPath))
            Assert.Ignore("set HC_MEMO_GRAMMAR (and either HC_PROBE_WORDS or HC_MEMO_WORDS)");

        int maxWords = int.TryParse(Environment.GetEnvironmentVariable("HC_MEMO_MAX_WORDS"), out int mw) ? mw : 60;
        List<string> fileWords = File.ReadAllLines(wordsPath!)
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .Take(maxWords)
            .ToList();
        return (language, fileWords);
    }
}
