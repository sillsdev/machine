#nullable enable
using System.Diagnostics;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// A measurement run, not a gate: for every one of <see cref="OrderingGenerator.EnumerateAdjacentPairs"/>'s
/// 138 adjacent-transposition items, this evaluates the fixture's baseline once and the swapped grammar
/// once, in a child <c>hc-conformance.dll --evaluate-mutant</c> process exactly like
/// <see cref="CounterfactualGate"/>'s own sweep does, and classifies each item as Evidenced, Proven
/// (disjoint-domains), a Gap (no delta and the static check cannot prove independence), a LoadFailure, or
/// a Timeout. Deliberately does not touch <see cref="CounterfactualGate"/>, <see cref="GrammarMutator"/>,
/// <see cref="CounterfactualLedger"/>, <see cref="ImpossibilityProofs"/>, or
/// <see cref="OrderingGenerator"/>: this only calls their public surface. Writes a full report to a
/// scratch file (path configurable via ORDERING_MEASUREMENT_REPORT) because the GAP list -- the thing
/// this run exists to produce -- is too long to usefully assert on.
/// </summary>
[TestFixture]
public sealed class OrderingCounterfactualMeasurementTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    private enum ItemClass
    {
        Evidenced,
        Proven,
        Gap,
        LoadFailure,
        Timeout,
    }

    private sealed record ItemResult(
        OrderingItem Item,
        ItemClass Class,
        DomainRelation? Relation,
        string Reason,
        string? DiffWord,
        string? Before,
        string? After,
        int? DiffIndex,
        int TotalWords,
        long EvaluationMs,
        string? MutatedGrammarPath
    );

    // Deliberately not tied to CounterfactualGate.DefaultTimeout: this run's child processes carry no
    // in-process fallback path, so a genuinely slow (not hung) mutant should not be misreported as a
    // Timeout.
    private static readonly TimeSpan EvaluationTimeout = TimeSpan.FromSeconds(30);

    [Test]
    [Explicit(
        "Measurement run, not a CI gate: shells out to hc-conformance.dll once per fixture baseline plus "
            + "once per one of the 138 ordering adjacent-swap items (~166 child processes total, sequential, "
            + "each independently killable on timeout). Takes on the order of a minute; run manually."
    )]
    public void MeasureOrderingAdjacentSwapCoverage()
    {
        string root = RepositoryRoot();
        string dllPath = Path.Combine(
            root,
            "src",
            "SIL.Machine.Morphology.HermitCrab.Conformance",
            "bin",
            "Debug",
            "net10.0",
            "hc-conformance.dll"
        );
        Assert.That(File.Exists(dllPath), Is.True, $"build the Conformance project first (dotnet build): {dllPath} not found");

        string scratchRoot =
            Environment.GetEnvironmentVariable("ORDERING_MEASUREMENT_SCRATCH")
            ?? Path.Combine(Path.GetTempPath(), "hc-ordering-measurement");
        if (Directory.Exists(scratchRoot))
            Directory.Delete(scratchRoot, recursive: true);
        Directory.CreateDirectory(scratchRoot);

        string reportPath =
            Environment.GetEnvironmentVariable("ORDERING_MEASUREMENT_REPORT")
            ?? Path.Combine(scratchRoot, "ordering-measurement-report.txt");

        List<Fixture> fixtures = Fixture.DiscoverAll(Path.Combine(root, "conformance"));
        var results = new List<ItemResult>();
        var perFixtureBaselineMs = new List<(string FixtureId, int Items, int Words, long BaselineMs)>();
        int evaluationCount = 0;
        int totalPairsSeen = 0;
        var wallClock = Stopwatch.StartNew();

        foreach (Fixture fixture in fixtures)
        {
            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            IReadOnlyList<OrderingItem> items = OrderingGenerator.EnumerateAdjacentPairs(grammar, fixture.Id);
            totalPairsSeen += items.Count;
            if (items.Count == 0)
                continue;

            string[] words = fixture.Words.Words.Select(w => w.Word).ToArray();
            string wordsPath = Path.Combine(scratchRoot, $"words-{Sanitize(fixture.Id)}.txt");
            File.WriteAllLines(wordsPath, words);

            TestContext.Out.WriteLine($"[{fixture.Id}] {items.Count} item(s), {words.Length} word(s)");

            IReadOnlyList<string> baseline;
            var baselineTimer = Stopwatch.StartNew();
            try
            {
                baseline = RunEvaluateMutant(dllPath, fixture.GrammarPath, wordsPath, EvaluationTimeout);
                evaluationCount++;
                baselineTimer.Stop();
                perFixtureBaselineMs.Add((fixture.Id, items.Count, words.Length, baselineTimer.ElapsedMilliseconds));
                TestContext.Out.WriteLine($"  baseline: {baselineTimer.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                evaluationCount++;
                TestContext.Out.WriteLine($"  *** BASELINE ITSELF FAILED: {ex.GetType().Name}: {ex.Message} ***");
                foreach (OrderingItem item in items)
                {
                    results.Add(
                        new ItemResult(
                            item,
                            ItemClass.LoadFailure,
                            null,
                            $"the fixture's own baseline failed to evaluate: {ex.GetType().Name}: {ex.Message}",
                            null,
                            null,
                            null,
                            null,
                            words.Length,
                            baselineTimer.ElapsedMilliseconds,
                            null
                        )
                    );
                }
                continue;
            }

            foreach (OrderingItem item in items)
            {
                OrderingSwap? swap = OrderingGenerator.Swap(grammar, item);
                Assert.That(swap, Is.Not.Null, $"item {item.Id} did not match its own fixture's grammar");

                string mutatedPath = Path.Combine(scratchRoot, $"mutant-{Sanitize(item.Id)}.xml");
                swap!.Mutated.Save(mutatedPath);

                var swapTimer = Stopwatch.StartNew();
                try
                {
                    IReadOnlyList<string> mutated = RunEvaluateMutant(dllPath, mutatedPath, wordsPath, EvaluationTimeout);
                    evaluationCount++;
                    swapTimer.Stop();

                    int diffIndex = -1;
                    for (int i = 0; i < words.Length && i < baseline.Count && i < mutated.Count; i++)
                    {
                        if (baseline[i] != mutated[i])
                        {
                            diffIndex = i;
                            break;
                        }
                    }

                    if (diffIndex >= 0)
                    {
                        results.Add(
                            new ItemResult(
                                item,
                                ItemClass.Evidenced,
                                null,
                                $"'{words[diffIndex]}': {baseline[diffIndex]} -> {mutated[diffIndex]}",
                                words[diffIndex],
                                baseline[diffIndex],
                                mutated[diffIndex],
                                diffIndex,
                                words.Length,
                                swapTimer.ElapsedMilliseconds,
                                mutatedPath
                            )
                        );
                    }
                    else if (baseline.Count != mutated.Count)
                    {
                        // Outcome-line count itself differs -- a real delta the word-by-word loop above
                        // cannot index into, so it must never fall through to a disjoint-domains check.
                        results.Add(
                            new ItemResult(
                                item,
                                ItemClass.Evidenced,
                                null,
                                $"outcome line count differs: baseline {baseline.Count} vs mutant {mutated.Count}",
                                null,
                                null,
                                null,
                                null,
                                words.Length,
                                swapTimer.ElapsedMilliseconds,
                                mutatedPath
                            )
                        );
                    }
                    else
                    {
                        DisjointDomainsCheck check = OrderingGenerator.CheckDisjointDomains(grammar, item);
                        ItemClass cls = check.Relation == DomainRelation.Disjoint ? ItemClass.Proven : ItemClass.Gap;
                        results.Add(
                            new ItemResult(
                                item,
                                cls,
                                check.Relation,
                                check.Reason,
                                null,
                                null,
                                null,
                                null,
                                words.Length,
                                swapTimer.ElapsedMilliseconds,
                                mutatedPath
                            )
                        );
                    }
                }
                catch (TimeoutException tex)
                {
                    evaluationCount++;
                    swapTimer.Stop();
                    TestContext.Out.WriteLine($"  *** TIMEOUT *** {item.Id}: {tex.Message}");
                    results.Add(
                        new ItemResult(
                            item,
                            ItemClass.Timeout,
                            null,
                            tex.Message,
                            null,
                            null,
                            null,
                            null,
                            words.Length,
                            swapTimer.ElapsedMilliseconds,
                            mutatedPath
                        )
                    );
                }
                catch (Exception ex)
                {
                    evaluationCount++;
                    swapTimer.Stop();
                    results.Add(
                        new ItemResult(
                            item,
                            ItemClass.LoadFailure,
                            null,
                            $"{ex.GetType().Name}: {ex.Message}",
                            null,
                            null,
                            null,
                            null,
                            words.Length,
                            swapTimer.ElapsedMilliseconds,
                            mutatedPath
                        )
                    );
                }
            }
        }

        wallClock.Stop();
        Assert.That(totalPairsSeen, Is.EqualTo(138), "the corpus-wide pair count must match the design doc's measured figure");
        Assert.That(results, Has.Count.EqualTo(138));

        WriteReport(reportPath, results, perFixtureBaselineMs, wallClock.Elapsed, evaluationCount);
        TestContext.Out.WriteLine();
        TestContext.Out.WriteLine($"full report written to {reportPath}");

        int timeouts = results.Count(r => r.Class == ItemClass.Timeout);
        if (timeouts > 0)
            TestContext.Out.WriteLine($"*** {timeouts} TIMEOUT(S) -- SHOULD NOT HAPPEN, SEE REPORT ***");
    }

    private static string Sanitize(string id)
    {
        char[] chars = id.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        }
        return new string(chars);
    }

    /// <summary>
    /// Runs one <c>--evaluate-mutant</c> child process exactly as the design's cost section and the
    /// task's own instructions specify: <c>DOTNET_PROCESSOR_COUNT=1 dotnet hc-conformance.dll
    /// --evaluate-mutant &lt;grammar&gt; &lt;words&gt;</c>. A separate implementation from
    /// <c>CounterfactualGate</c>'s private <c>OutcomesWithTimeout</c> (that file is owned by concurrent
    /// work) but the same shape: drain both pipes before waiting so a full one never deadlocks the wait,
    /// kill the whole tree on timeout, and never let stderr's TIME lines leak into the outcome list.
    /// </summary>
    private static IReadOnlyList<string> RunEvaluateMutant(string dllPath, string grammarPath, string wordsPath, TimeSpan timeout)
    {
        var start = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(dllPath);
        start.ArgumentList.Add("--evaluate-mutant");
        start.ArgumentList.Add(grammarPath);
        start.ArgumentList.Add(wordsPath);
        start.EnvironmentVariables["DOTNET_PROCESSOR_COUNT"] = "1";

        ChildProcessHarness.Result result;
        try
        {
            result = ChildProcessHarness.Run(start, CancellationToken.None, backstop: timeout);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"'{grammarPath}' did not complete within {timeout.TotalSeconds:0}s");
        }

        if (result.ExitCode != 0)
        {
            string error = result.StandardError.Trim();
            throw new InvalidOperationException(error.Length != 0 ? error : $"exit code {result.ExitCode}");
        }

        return result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.TrimEnd('\r')).ToArray();
    }

    private static string Cause(ItemResult result)
    {
        if (result.Item.Kind == OrderingListKind.AffixTemplateSlots)
            return "template-slot pair (AffixTemplate Slot ordering)";
        if (result.Item.Kind == OrderingListKind.StratumMorphologicalRules)
            return "morphological-rule pair (Stratum morphologicalRules)";

        // StratumPhonologicalRules: CheckDisjointDomains resolves these, so the reason text names why
        // it fell through to Undetermined rather than resolving to Disjoint/Overlaps.
        string reason = result.Reason;
        if (reason.Contains("MetathesisRule", StringComparison.Ordinal))
            return "MetathesisRule member (StructuralDescription not modeled)";
        if (reason.Contains("not a modeled phonetic-sequence construct", StringComparison.Ordinal))
            return "raw phonetic-sequence construct (e.g. <Segments>) not modeled";
        if (reason.Contains("was not found as a PhonologicalRule/MetathesisRule", StringComparison.Ordinal))
            return "member id not resolvable to a PhonologicalRule/MetathesisRule";
        if (reason.Contains("no PhonologicalSubrule children found", StringComparison.Ordinal))
            return "PhonologicalRule with no PhonologicalSubrule children";
        if (reason.Contains("is not declared in this document", StringComparison.Ordinal))
            return "natural class id not declared in the document";
        if (reason.Contains("nested FeatureValue", StringComparison.Ordinal))
            return "complex-feature (nested FeatureValue) matching not modeled";
        if (reason.Contains("does not declare every feature", StringComparison.Ordinal))
            return "SegmentDefinition missing a constrained feature";
        if (result.Relation == DomainRelation.Overlaps)
            return "resolved but genuinely overlapping (real interaction, weak words)";
        return $"other phonologicalRules Undetermined reason: {Truncate(reason, 80)}";
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";

    private static void WriteReport(
        string path,
        List<ItemResult> results,
        List<(string FixtureId, int Items, int Words, long BaselineMs)> baselines,
        TimeSpan wallClock,
        int evaluationCount
    )
    {
        using var writer = new StreamWriter(path, false);
        void WL(string s = "")
        {
            writer.WriteLine(s);
        }

        int evidenced = results.Count(r => r.Class == ItemClass.Evidenced);
        int proven = results.Count(r => r.Class == ItemClass.Proven);
        int gap = results.Count(r => r.Class == ItemClass.Gap);
        int loadFailure = results.Count(r => r.Class == ItemClass.LoadFailure);
        int timeout = results.Count(r => r.Class == ItemClass.Timeout);

        WL("ORDERING ADJACENT-SWAP COUNTERFACTUAL MEASUREMENT");
        WL("===================================================");
        WL();
        WL($"total items: {results.Count} (design doc's measured figure: 138)");
        WL($"  EVIDENCED    {evidenced}");
        WL($"  PROVEN       {proven}  (disjoint-domains)");
        WL($"  GAP          {gap}  (no delta, static check Undetermined or Overlaps)");
        WL($"  LOAD_FAILURE {loadFailure}");
        WL($"  TIMEOUT      {timeout}");
        WL();

        WL("COST");
        WL("----");
        WL($"total wall clock: {wallClock.TotalSeconds:0.0}s");
        WL($"evaluations run (baselines + swaps): {evaluationCount}");
        WL($"average per evaluation: {(evaluationCount == 0 ? 0 : wallClock.TotalMilliseconds / evaluationCount):0.0}ms");
        long totalSwapMs = results.Sum(r => r.EvaluationMs);
        WL($"total swap-evaluation time (excludes baselines): {totalSwapMs}ms across {results.Count} swap(s), average {(results.Count == 0 ? 0 : (double)totalSwapMs / results.Count):0.0}ms/swap");
        long totalBaselineMs = baselines.Sum(b => b.BaselineMs);
        WL($"total baseline time: {totalBaselineMs}ms across {baselines.Count} fixture(s), average {(baselines.Count == 0 ? 0 : (double)totalBaselineMs / baselines.Count):0.0}ms/fixture");
        WL();
        WL("per-fixture baseline cost:");
        foreach (var b in baselines.OrderByDescending(b => b.BaselineMs))
            WL($"  {b.FixtureId,-55} {b.Items,3} item(s) {b.Words,4} word(s) {b.BaselineMs,6}ms");
        WL();

        WL("FIRST-DIFFERING-WORD ANALYSIS (evidenced items only)");
        WL("-----------------------------------------------------");
        ItemResult[] evidencedItems = results.Where(r => r.Class == ItemClass.Evidenced && r.DiffIndex.HasValue).ToArray();
        if (evidencedItems.Length == 0)
        {
            WL("no evidenced items carried a word-level diff index (all deltas were outcome-count mismatches).");
        }
        else
        {
            int totalWordsAcrossEvidenced = evidencedItems.Sum(r => r.TotalWords);
            int wordsThatWouldHaveBeenSkipped = evidencedItems.Sum(r => r.TotalWords - (r.DiffIndex!.Value + 1));
            double fractionSkippable = totalWordsAcrossEvidenced == 0
                ? 0
                : (double)wordsThatWouldHaveBeenSkipped / totalWordsAcrossEvidenced;
            WL($"evidenced items with a word-level diff index: {evidencedItems.Length}");
            WL($"total words parsed across those items (baseline+mutant each parse every word): {totalWordsAcrossEvidenced}");
            WL($"words that a stop-at-first-difference optimization would have skipped (mutant side only): {wordsThatWouldHaveBeenSkipped}");
            WL($"fraction of words skippable across evidenced items: {fractionSkippable:P1}");
            WL();
            WL("per-item detail (diff index / total words):");
            foreach (ItemResult r in evidencedItems.OrderBy(r => r.Item.Id, StringComparer.Ordinal))
            {
                WL(
                    $"  {r.Item.Id,-90} diffIndex={r.DiffIndex} of {r.TotalWords} word(s) "
                        + $"(skippable={r.TotalWords - (r.DiffIndex!.Value + 1)})"
                );
            }
        }
        WL();

        WL("EVIDENCED ITEMS (full list)");
        WL("---------------------------");
        foreach (ItemResult r in results.Where(r => r.Class == ItemClass.Evidenced).OrderBy(r => r.Item.Id, StringComparer.Ordinal))
        {
            WL($"  {r.Item.Id}");
            WL($"    fixture: {r.Item.FixtureId}  kind: {r.Item.Kind}  pair: {r.Item.MemberA} <-> {r.Item.MemberB}");
            WL($"    delta: {r.Reason}");
            WL($"    mutantGrammar: {r.MutatedGrammarPath}");
        }
        WL();

        WL("LOAD_FAILURE ITEMS");
        WL("-------------------");
        foreach (ItemResult r in results.Where(r => r.Class == ItemClass.LoadFailure).OrderBy(r => r.Item.Id, StringComparer.Ordinal))
        {
            WL($"  {r.Item.Id}");
            WL($"    fixture: {r.Item.FixtureId}  pair: {r.Item.MemberA} <-> {r.Item.MemberB}");
            WL($"    exception: {r.Reason}");
        }
        WL();

        WL("TIMEOUT ITEMS");
        WL("-------------");
        foreach (ItemResult r in results.Where(r => r.Class == ItemClass.Timeout).OrderBy(r => r.Item.Id, StringComparer.Ordinal))
        {
            WL($"  {r.Item.Id}  fixture: {r.Item.FixtureId}  pair: {r.Item.MemberA} <-> {r.Item.MemberB}");
            WL($"    {r.Reason}");
        }
        WL();

        WL("GAP LIST -- grouped by cause (the backlog for finishing ordering coverage)");
        WL("===========================================================================");
        var byCause = results
            .Where(r => r.Class == ItemClass.Gap)
            .GroupBy(Cause)
            .OrderByDescending(g => g.Count());
        foreach (var group in byCause)
        {
            WL();
            WL($"-- {group.Key} ({group.Count()}) --");
            foreach (ItemResult r in group.OrderBy(r => r.Item.Id, StringComparer.Ordinal))
            {
                WL($"  {r.Item.Id}");
                WL($"    fixture: {r.Item.FixtureId}  kind: {r.Item.Kind}  pair: {r.Item.MemberA} <-> {r.Item.MemberB}");
                WL($"    static-check relation: {r.Relation}  reason: {r.Reason}");
            }
        }
        WL();

        WL("PROVEN ITEMS (disjoint-domains, full list)");
        WL("-------------------------------------------");
        foreach (ItemResult r in results.Where(r => r.Class == ItemClass.Proven).OrderBy(r => r.Item.Id, StringComparer.Ordinal))
        {
            WL($"  {r.Item.Id}");
            WL($"    pair: {r.Item.MemberA} <-> {r.Item.MemberB}  reason: {r.Reason}");
        }
    }
}
