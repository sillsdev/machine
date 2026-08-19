#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Runs <see cref="CounterfactualGate"/> across every fixture and reads/writes the checked-in
/// ledger, so the CLI and the test suite recompute the identical thing rather than each carrying
/// its own copy of the sweep.
/// </summary>
public static class CounterfactualLedger
{
    public const string RelativePath = "conformance/semantic-coverage-counterfactuals.tsv";

    /// <summary>
    /// Evaluates every surface every non-pathological, non-crash fixture contains, and keeps the
    /// strongest verdict any fixture reaches for each surface.
    /// </summary>
    public static IReadOnlyList<CounterfactualResult> Sweep(
        string repositoryRoot,
        SemanticInventory inventory,
        Action<string, int>? onFixtureStarted = null,
        Action<WordTiming>? onWordTimed = null
    )
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(inventory);
        string scratch = Path.Combine(Path.GetTempPath(), "hc-counterfactual");
        var results = new List<CounterfactualResult>();

        // Every fixture's baseline has to be computed regardless -- evaluating any of its surfaces
        // needs it -- so timing it here to run fixtures cheapest-first afterward costs nothing extra.
        var candidates =
            new List<(Fixture Fixture, string[] SurfaceIds, IReadOnlyList<string> Baseline, long BaselineMs)>();
        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            // Neither a pathological timing fixture nor a crash-is-the-answer fixture has a real
            // outcome CI ever checks, so there is no trustworthy baseline to diff a mutant against.
            if (fixture.Words.BudgetMs is not null || fixture.Words.ExpectCrash)
                continue;

            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            string[] surfaceIds = GrammarFeatureUsage
                .Read(grammar, inventory)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
            if (surfaceIds.Length == 0)
                continue;

            IReadOnlyList<string> baseline;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                baseline = CounterfactualGate.ComputeBaseline(
                    fixture,
                    onTimed: (word, ms) => onWordTimed?.Invoke(new WordTiming("baseline", fixture.Id, null, word, ms))
                );
            }
            catch (Exception ex)
            {
                foreach (string surfaceId in surfaceIds)
                {
                    results.Add(
                        new CounterfactualResult(
                            surfaceId,
                            fixture.Id,
                            CounterfactualVerdict.Unobservable,
                            "none",
                            $"the fixture itself does not load: {ex.GetType().Name}"
                        )
                    );
                }
                continue;
            }
            stopwatch.Stop();
            candidates.Add((fixture, surfaceIds, baseline, stopwatch.ElapsedMilliseconds));
        }

        // Deliberately exhaustive: every fixture is evaluated against every surface it contains, in
        // discovery order, with no short-circuit once some fixture already reaches the best possible
        // verdict for a surface. A skip there would mean a surface's later fixtures are never even
        // run, so a languages/* fixture that ALSO reaches Evidenced could never be discovered once an
        // edge-case fixture (sorted first by Fixture.DiscoverAll) got there first -- exactly the
        // fold-in-candidates.tsv defect this ledger exists to avoid. See the tie-preserving merge below.
        foreach (var candidate in candidates)
        {
            onFixtureStarted?.Invoke(candidate.Fixture.Id, candidate.SurfaceIds.Length);
            foreach (string surfaceId in candidate.SurfaceIds)
            {
                CounterfactualResult result = CounterfactualGate.Evaluate(
                    candidate.Fixture,
                    surfaceId,
                    inventory,
                    candidate.Baseline,
                    scratch,
                    onWordTimed: (word, ms) =>
                        onWordTimed?.Invoke(new WordTiming("mutant", candidate.Fixture.Id, surfaceId, word, ms))
                );
                results.Add(result);

                // A single flip proved nothing; before giving up, see whether an independent
                // referencing declaration exists that a JOINT flip can pin the delta on instead. Only
                // worth the extra runs when the single-surface attempt was genuinely inconclusive.
                if (result.Verdict == CounterfactualVerdict.Unobservable)
                {
                    CounterfactualResult jointResult = CounterfactualGate.EvaluateJointly(
                        candidate.Fixture,
                        surfaceId,
                        inventory,
                        candidate.Baseline,
                        scratch,
                        onWordTimed: (word, ms) =>
                            onWordTimed?.Invoke(new WordTiming("mutant", candidate.Fixture.Id, surfaceId, word, ms))
                    );
                    results.Add(jointResult);
                }
            }
        }

        // The strongest verdict any fixture reaches for a surface is the one that counts, and every
        // fixture tied at that verdict is a real witness -- recording only the first-discovered one
        // (candidates arrive in Fixture.DiscoverAll's id order, so edge-cases always precede
        // languages/*) would silently drop a language-grammar witness that ties rather than beats it.
        // The kept row is still the first-discovered (its example/mutation fields describe that one
        // run), but WitnessingFixtures names every fixture that reached the same verdict.
        var bestVerdictBySurface = new Dictionary<string, CounterfactualVerdict>(StringComparer.Ordinal);
        foreach (CounterfactualResult result in results)
        {
            if (!bestVerdictBySurface.TryGetValue(result.SurfaceId, out CounterfactualVerdict existing) || result.Verdict < existing)
                bestVerdictBySurface[result.SurfaceId] = result.Verdict;
        }

        var tiedFixturesBySurface = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (CounterfactualResult result in results)
        {
            if (result.Verdict != bestVerdictBySurface[result.SurfaceId])
                continue;
            if (!tiedFixturesBySurface.TryGetValue(result.SurfaceId, out SortedSet<string>? fixtureIds))
            {
                fixtureIds = new SortedSet<string>(StringComparer.Ordinal);
                tiedFixturesBySurface[result.SurfaceId] = fixtureIds;
            }
            fixtureIds.Add(result.FixtureId);
        }

        var best = new SortedDictionary<string, CounterfactualResult>(StringComparer.Ordinal);
        foreach (CounterfactualResult result in results)
        {
            if (result.Verdict != bestVerdictBySurface[result.SurfaceId] || best.ContainsKey(result.SurfaceId))
                continue;
            best[result.SurfaceId] = result with { WitnessingFixtures = tiedFixturesBySurface[result.SurfaceId].ToArray() };
        }

        return best.Values.ToArray();
    }

    /// <summary>
    /// Evaluates every Ordering adjacent-pair item the corpus declares (<see
    /// cref="OrderingGenerator.EnumerateAdjacentPairs"/>) by swapping it and diffing against its own
    /// fixture's baseline. Unlike <see cref="Sweep"/>, no cross-fixture "best verdict" merge is needed:
    /// an Ordering item's id already names the one fixture it came from, so each item is evaluated
    /// exactly once. The baseline is computed through the same killable child process a mutant uses
    /// (<see cref="CounterfactualGate.EvaluateWithTimeout"/>), never in-process, because some fixtures in
    /// this corpus are deliberately pathological or expect_crash and have no trustworthy unprotected run.
    /// </summary>
    public static IReadOnlyList<CounterfactualResult> SweepOrdering(
        string repositoryRoot,
        Action<string, int>? onFixtureStarted = null,
        Action<WordTiming>? onWordTimed = null
    )
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        string scratch = Path.Combine(Path.GetTempPath(), "hc-ordering-counterfactual");
        var results = new List<CounterfactualResult>();

        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            IReadOnlyList<OrderingItem> items = OrderingGenerator.EnumerateAdjacentPairs(grammar, fixture.Id);
            if (items.Count == 0)
                continue;

            onFixtureStarted?.Invoke(fixture.Id, items.Count);
            string[] words = fixture.Words.Words.Select(word => word.Word).ToArray();

            IReadOnlyList<string> baseline;
            try
            {
                baseline = CounterfactualGate.EvaluateWithTimeout(
                    fixture.GrammarPath,
                    words,
                    onTimed: (word, ms) => onWordTimed?.Invoke(new WordTiming("baseline", fixture.Id, null, word, ms))
                );
            }
            catch (TimeoutException)
            {
                foreach (OrderingItem item in items)
                {
                    results.Add(
                        new CounterfactualResult(
                            item.Id,
                            fixture.Id,
                            CounterfactualVerdict.Timeout,
                            "none",
                            "the fixture's own unmutated baseline did not terminate within the timeout"
                        )
                    );
                }
                continue;
            }
            catch (Exception ex)
            {
                foreach (OrderingItem item in items)
                {
                    results.Add(
                        new CounterfactualResult(
                            item.Id,
                            fixture.Id,
                            CounterfactualVerdict.Unobservable,
                            "none",
                            $"the fixture's own unmutated baseline failed to evaluate: {ex.GetType().Name}"
                        )
                    );
                }
                continue;
            }

            foreach (OrderingItem item in items)
            {
                results.Add(
                    CounterfactualGate.EvaluateOrderingSwap(
                        fixture,
                        item,
                        baseline,
                        scratch,
                        onWordTimed: (word, ms) =>
                            onWordTimed?.Invoke(new WordTiming("mutant", fixture.Id, item.Id, word, ms))
                    )
                );
            }
        }

        return results.OrderBy(r => r.SurfaceId, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Reads the checked-in ledger, or an empty list if it has never been written.</summary>
    public static IReadOnlyList<CounterfactualResult> Read(string repositoryRoot)
    {
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            return Array.Empty<CounterfactualResult>();

        var entries = new List<CounterfactualResult>();
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (
                line.Length == 0
                || line.StartsWith("#", StringComparison.Ordinal)
                || line.StartsWith("surface\t", StringComparison.Ordinal)
            )
            {
                continue;
            }
            string[] fields = line.Split('\t');
            if (fields.Length != 10)
                throw new FormatException($"{RelativePath}: '{line}' must be 10 tab-separated fields");
            if (!Enum.TryParse(fields[1], out CounterfactualVerdict verdict))
                throw new FormatException($"{RelativePath}: unknown verdict '{fields[1]}' for '{fields[0]}'");
            if (!Enum.TryParse(fields[7], out CounterexampleKind counterexampleKind))
                throw new FormatException($"{RelativePath}: unknown counterexample kind '{fields[7]}' for '{fields[0]}'");
            entries.Add(
                new CounterfactualResult(
                    fields[0],
                    fields[2],
                    verdict,
                    fields[3],
                    fields[4],
                    fields[5] == NullField ? null : fields[5],
                    fields[6] == NullField ? null : fields[6],
                    counterexampleKind,
                    fields[8] == NullField ? null : fields[8],
                    fields[9].Split(',')
                )
            );
        }

        return entries.OrderBy(entry => entry.SurfaceId, StringComparer.Ordinal).ToArray();
    }

    private const string NullField = "-";

    public static void Write(string repositoryRoot, IReadOnlyList<CounterfactualResult> entries)
    {
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        using var writer = new StreamWriter(path, false);
        writer.WriteLine("# GENERATED by hc-conformance --write-counterfactual. One line per grammar-observable");
        writer.WriteLine("# surface a fixture contains: the verdict, the fixture, what was neutralized, the");
        writer.WriteLine("# observed delta, and the structural counter-example fields CoverageEvidencePipeline");
        writer.WriteLine("# joins into an Evidence record (never re-derived from delta's free text). Absent");
        writer.WriteLine("# fields are \"-\".");
        writer.WriteLine("#   Evidenced        neutralizing it changed a result");
        writer.WriteLine("#   RequiredByLoader the mutant would not load: HermitCrab's own loader threw (an");
        writer.WriteLine("#                    IDREF/lookup/coercion failure) after passing DTD validation --");
        writer.WriteLine("#                    a real engine-semantic witness, though not a word-level delta");
        writer.WriteLine("#   EvidencedJointly only a joint mutation with an independent partner changed a result;");
        writer.WriteLine(
            "#                    weaker than Evidenced/RequiredByLoader, see CounterfactualGate.EvaluateJointly"
        );
        writer.WriteLine("#   RequiredByDtd    the mutant would not load: it failed generic DTD content-model");
        writer.WriteLine("#                    validation before HermitCrab's loader ran at all -- this only");
        writer.WriteLine("#                    re-derives Level 1's static DTD enumeration and is NOT equally");
        writer.WriteLine("#                    conclusive with Evidenced; there is no parse-time witness");
        writer.WriteLine("#   Timeout          the mutant did not finish in time, which is not evidence");
        writer.WriteLine("#   Unobservable     neutralizing it changed nothing, which is not evidence");
        writer.WriteLine("# fixture is the first-discovered fixture reaching verdict (its run is what mutation/");
        writer.WriteLine("# delta/example describe); witnessed_by is EVERY fixture that reached the identical");
        writer.WriteLine("# verdict, comma-separated, ALWAYS including fixture -- a tie is not a loss, see");
        writer.WriteLine("# CounterfactualLedger.Sweep. FoldInCandidateLedger reads this to tell 'only an edge");
        writer.WriteLine("# case witnesses this' from 'an edge case was merely recorded first'.");
        writer.WriteLine(
            "surface\tverdict\tfixture\tmutation\tdelta\texample_word\texample_outcome\tcounterexample_kind\tcounterexample_outcome\twitnessed_by"
        );
        foreach (CounterfactualResult r in entries.OrderBy(entry => entry.SurfaceId, StringComparer.Ordinal))
        {
            writer.WriteLine(
                string.Join(
                    '\t',
                    r.SurfaceId,
                    r.Verdict,
                    r.FixtureId,
                    r.Mutation,
                    r.Delta,
                    r.ExampleWord ?? NullField,
                    r.ExampleOutcome ?? NullField,
                    r.CounterexampleKind,
                    r.CounterexampleOutcome ?? NullField,
                    string.Join(",", r.WitnessingFixtures ?? new[] { r.FixtureId })
                )
            );
        }
    }
}
