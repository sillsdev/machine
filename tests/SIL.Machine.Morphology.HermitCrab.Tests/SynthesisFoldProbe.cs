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
        var fixtureRatios = new List<FixtureSummaryRow>();
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

                // Corrected share for fold-step sharing (docs/hermitcrab-synthesis-fold-probes.md section
                // 6.4): synForward is explicitly NET of the cascade/battery brackets (Morpher.cs:424), but
                // every application P1c counts happens inside SynthesisAffixProcessRule.Apply /
                // SynthesisRealizationalAffixProcessRule.Apply, which run INSIDE the synCascade/synBattery
                // brackets (template slot rules compile to those same classes via RuleBatch). So the
                // shareable work lives in synCascade + synBattery + synForward, not synForward alone --
                // dividing by synForward alone is the error section 6.4 found and corrected.
                double fixtureWall = rows.Sum(r => r.WallMs);
                double fixtureSynCascade = rows.Sum(r => r.SynCascadeMs);
                double fixtureSynBattery = rows.Sum(r => r.SynBatteryMs);
                double fixtureSynForward = rows.Sum(r => r.SynForwardMs);
                double fixtureSynExpand = rows.Sum(r => r.SynExpandMs);
                double synTotalShare =
                    fixtureWall > 0
                        ? (fixtureSynCascade + fixtureSynBattery + fixtureSynForward) / fixtureWall
                        : 0;
                double synExpandShare = fixtureWall > 0 ? fixtureSynExpand / fixtureWall : 0;
                // share x (1 - 1/ratio), as a percentage (docs section 6.4's "corrected ceilings" formula).
                double foldStepCeiling = ratio > 0 ? synTotalShare * (1 - 1 / ratio) * 100 : 0;

                // N1 fold-entry census, per fixture (SynthesisProbe.ResetFoldSteps above also resets the
                // fold-entry counters, so these are this fixture's own totals, not cumulative across
                // fixtures -- see section 6.4's "one honest gap" / dedupe census).
                long altTotal = SynthesisProbe.TotalAlternatives;
                long altDistinct = SynthesisProbe.DistinctAlternatives;
                double altDistinctPct = altTotal > 0 ? altDistinct / (double)altTotal * 100 : 0;
                long dupeSame = SynthesisProbe.DupeSameAnalysisWord;
                long dupeDifferent = SynthesisProbe.DupeDifferentAnalysisWord;
                long totalDupes = dupeSame + dupeDifferent;
                double dupeSameWordPct = totalDupes > 0 ? dupeSame / (double)totalDupes * 100 : 0;

                fixtureRatios.Add(
                    new FixtureSummaryRow
                    {
                        Id = fixture.Id,
                        Ratio = ratio,
                        Applications = applications,
                        Distinct = distinct,
                        SynTotalShare = synTotalShare,
                        SynExpandShare = synExpandShare,
                        FoldStepCeiling = foldStepCeiling,
                        AltTotal = altTotal,
                        AltDistinct = altDistinct,
                        AltDistinctPct = altDistinctPct,
                        DupeSameWordPct = dupeSameWordPct,
                        WallMs = fixtureWall,
                        Words = rows.Count,
                    }
                );
                grandDeterminismViolations += SynthesisProbe.DeterminismViolations;
            }

            fixtureRatios.Sort((a, b) => b.Ratio.CompareTo(a.Ratio));

            TestContext.Out.WriteLine();
            TestContext.Out.WriteLine(
                "=== P1c ratio by fixture (not pooled -- fixtures vary wildly in size), sorted by ratio "
                    + "descending; synTotalShare = (synCascade+synBattery+synForward)/wall (the corrected "
                    + "share, docs section 6.4); foldStepCeiling = synTotalShare x (1 - 1/ratio); "
                    + "reliable = wallMs >= 50 ==="
            );
            TestContext.Out.WriteLine(
                "  id\tapplications\tdistinct\tratio\tsynTotalShare\tsynExpandShare\tfoldStepCeiling\t"
                    + "altTotal\taltDistinct\taltDistinctPct\tdupeSameWordPct\twallMs\twords\treliable"
            );
            foreach (FixtureSummaryRow r in fixtureRatios)
            {
                TestContext.Out.WriteLine(
                    $"  {r.Id}\t{r.Applications}\t{r.Distinct}\t{r.Ratio:F2}x\t"
                        + $"{r.SynTotalShare * 100:F1}%\t{r.SynExpandShare * 100:F1}%\t{r.FoldStepCeiling:F2}%\t"
                        + $"{r.AltTotal}\t{r.AltDistinct}\t{r.AltDistinctPct:F2}%\t{r.DupeSameWordPct:F1}%\t"
                        + $"{r.WallMs:F2}\t{r.Words}\t{(r.WallMs >= 50 ? "yes" : "no")}"
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

    /// <summary>
    /// One row of the per-fixture summary table printed at the end of <see cref="Probe_ConformanceFixtures"/>.
    /// See docs/hermitcrab-synthesis-fold-probes.md section 6.4 for the corrected share formula this
    /// replaces the old (wrong) forwardShare/value columns with.
    /// </summary>
    private sealed class FixtureSummaryRow
    {
        public string Id;
        public double Ratio;
        public long Applications;
        public long Distinct;
        public double SynTotalShare;
        public double SynExpandShare;
        public double FoldStepCeiling;
        public long AltTotal;
        public long AltDistinct;
        public double AltDistinctPct;
        public double DupeSameWordPct;
        public double WallMs;
        public int Words;
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
        public double SynExpandMs;
        public double AnTotalMs;
        public double AnCascadeMs;
        public double AnBatteryMs;
        public double AnPhonoMs;
        public double UnaccountedMs;
        public long[] DieCounts;
        public long ApplicationsThisWord;
        public long NewDistinctThisWord;

        // N1 dedupe census (docs/hermitcrab-synthesis-fold-probes.md section 6.4's "one honest gap"):
        // alternatives entering the fold this word, how many were new distinct fingerprints, and of the
        // duplicates, how many trace their first occurrence to the same analysis word vs. a different one.
        public long AlternativesThisWord;
        public long NewDistinctAlternativesThisWord;
        public long DupeSameThisWord;
        public long DupeDifferentThisWord;

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
        long alternativesBefore = SynthesisProbe.TotalAlternatives;
        long distinctAltBefore = SynthesisProbe.DistinctAlternatives;
        long dupeSameBefore = SynthesisProbe.DupeSameAnalysisWord;
        long dupeDifferentBefore = SynthesisProbe.DupeDifferentAnalysisWord;

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
        double synExpandMs = TicksToMs(SynthesisProbe.SynExpandTicks);
        double anTotalMs = TicksToMs(SynthesisProbe.AnTotalTicks);
        double anCascadeMs = TicksToMs(SynthesisProbe.AnCascadeTicks);
        double anBatteryMs = TicksToMs(SynthesisProbe.AnBatteryTicks);
        double anPhonoMs = TicksToMs(SynthesisProbe.AnPhonoTicks);

        // Top-level buckets are disjoint by construction (see SynthesisProbe's wall-time-split remarks):
        // AnTotalMs is the whole analysis phase (a nested/inclusive total that already contains
        // AnCascade/AnBattery/AnPhono), and the syn*/lookup/synExpand buckets are disjoint slices of the
        // synthesis phase (SynForwardMs is already net of SynCascade/SynBattery, see
        // Morpher.SynthesizeSequential; SynExpandMs is N1's new bracket around ExpandAlternatives() itself,
        // also disjoint from all of those -- it wraps the call, not anything inside SynCascade/SynBattery/
        // SynForward). So unaccounted = wall - analysis phase - synthesis phase, i.e. ParseWord's own
        // scaffolding (shape segmentation, Word construction/Freeze, AccumulateMemoDiagnostics, guessRoot)
        // plus any region this probe does not yet bracket.
        double unaccountedMs = wallMs - anTotalMs - lookupMs - synCascadeMs - synBatteryMs - synForwardMs - synExpandMs;

        return new WordProbeResult
        {
            Word = word,
            ParseCount = parseCount,
            WallMs = wallMs,
            LexicalLookupMs = lookupMs,
            SynCascadeMs = synCascadeMs,
            SynBatteryMs = synBatteryMs,
            SynForwardMs = synForwardMs,
            SynExpandMs = synExpandMs,
            AnTotalMs = anTotalMs,
            AnCascadeMs = anCascadeMs,
            AnBatteryMs = anBatteryMs,
            AnPhonoMs = anPhonoMs,
            UnaccountedMs = unaccountedMs,
            DieCounts = dieCounts,
            ApplicationsThisWord = SynthesisProbe.TotalApplications - applicationsBefore,
            NewDistinctThisWord = SynthesisProbe.DistinctFoldSteps - distinctBefore,
            AlternativesThisWord = SynthesisProbe.TotalAlternatives - alternativesBefore,
            NewDistinctAlternativesThisWord = SynthesisProbe.DistinctAlternatives - distinctAltBefore,
            DupeSameThisWord = SynthesisProbe.DupeSameAnalysisWord - dupeSameBefore,
            DupeDifferentThisWord = SynthesisProbe.DupeDifferentAnalysisWord - dupeDifferentBefore,
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
            double wordRatio = r.NewDistinctAlternativesThisWord > 0
                ? r.AlternativesThisWord / (double)r.NewDistinctAlternativesThisWord
                : 0;
            TestContext.Out.WriteLine(
                $"  {r.Word}\tparses={r.ParseCount}\twall={r.WallMs:F2}ms\t"
                    + $"lookup={r.LexicalLookupMs:F2}\tsynCascade={r.SynCascadeMs:F2}\t"
                    + $"synBattery={r.SynBatteryMs:F2}\tsynForward={r.SynForwardMs:F2}\tsynExpand={r.SynExpandMs:F2}\t"
                    + $"anTotal={r.AnTotalMs:F2} [anCascade={r.AnCascadeMs:F2} anBattery={r.AnBatteryMs:F2} anPhono={r.AnPhonoMs:F2}]\t"
                    + $"unaccounted={r.UnaccountedMs:F2}\t"
                    + $"apps+={r.ApplicationsThisWord}\tnewDistinct+={r.NewDistinctThisWord}\t"
                    + $"[N1] alternatives+={r.AlternativesThisWord}\tdistinctAlternatives+={r.NewDistinctAlternativesThisWord}"
                    + $"\tdistinct/total={Pct(r.NewDistinctAlternativesThisWord, r.AlternativesThisWord)} (ratio={wordRatio:F2}x)"
                    + $"\tdupeSame={r.DupeSameThisWord}\tdupeDifferent={r.DupeDifferentThisWord}"
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
        double sumSynExpand = rows.Sum(r => r.SynExpandMs);
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
                + $"synExpand={sumSynExpand:F2}ms ({Pct(sumSynExpand, sumWall)}) "
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

        // N1 dedupe census, cumulative (docs/hermitcrab-synthesis-fold-probes.md section 6.4's "one honest
        // gap"). distinctAlternatives/totalAlternatives is the gate's "distinct/total" ratio; dupeSame vs.
        // dupeDifferent is the decisive provenance split -- same-analysis-word duplication is interceptable
        // BEFORE ExpandAlternatives' Clone/Unify/Freeze work, cross-analysis-word duplication only AFTER it.
        long totalAlternatives = SynthesisProbe.TotalAlternatives;
        long distinctAlternatives = SynthesisProbe.DistinctAlternatives;
        long dupeSame = SynthesisProbe.DupeSameAnalysisWord;
        long dupeDifferent = SynthesisProbe.DupeDifferentAnalysisWord;
        double distinctOverTotal = totalAlternatives > 0 ? distinctAlternatives / (double)totalAlternatives : 0;
        long totalDupes = dupeSame + dupeDifferent;
        TestContext.Out.WriteLine(
            $"  [N1] totalAlternatives={totalAlternatives} distinctAlternatives={distinctAlternatives} "
                + $"distinct/total={distinctOverTotal:F3} ({Pct(distinctAlternatives, totalAlternatives)}) "
                + $"dupeSameAnalysisWord={dupeSame} ({Pct(dupeSame, totalDupes)} of dupes) "
                + $"dupeDifferentAnalysisWord={dupeDifferent} ({Pct(dupeDifferent, totalDupes)} of dupes) "
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
