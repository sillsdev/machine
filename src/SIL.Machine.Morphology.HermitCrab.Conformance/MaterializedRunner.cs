using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

public enum FixtureOutcome
{
    Passed,
    Failed,
    Skipped,
}

public class FixtureResult
{
    public string FixtureId = "";
    public FixtureOutcome Outcome;
    public string Reason = "";
    public long ElapsedMs;
    public List<WordResult> WordResults = new();
}

public class MaterializedRunReport
{
    public List<FixtureResult> Results { get; } = new();
    public int ExcludedPathologicalCount { get; set; }

    public int Passed => Results.Count(r => r.Outcome == FixtureOutcome.Passed);
    public int Failed => Results.Count(r => r.Outcome == FixtureOutcome.Failed);
    public int Skipped => Results.Count(r => r.Outcome == FixtureOutcome.Skipped);
}

/// <summary>
/// Runs a list of <see cref="MaterializedFixture"/>s through an <see cref="IEngine"/>, applying
/// category exclusion (pathological) and capability filtering (manifest "requires" vs. the engine's
/// declared capabilities), and diffs results against each fixture's expected.tsv. Called by
/// <see cref="Runner.RunAdapter"/> against fixtures <see cref="FixtureMaterializer"/> writes to disk.
/// </summary>
public static class MaterializedRunner
{
    public static MaterializedRunReport Run(
        List<MaterializedFixture> fixtures,
        IEngine engine,
        bool includePathological
    )
    {
        var report = new MaterializedRunReport();

        foreach (MaterializedFixture fixture in fixtures)
        {
            // category:pathological is the authoritative signal (the only thing docs ever mention);
            // the manifest's own "pathological" boolean is redundant and, if present, must agree
            // with it -- a mismatch is a manifest bug, not a runtime condition to silently resolve.
            bool isPathological = fixture.Manifest.Category == "pathological";
            var result = new FixtureResult { FixtureId = fixture.Id };
            if (fixture.Manifest.Pathological.HasValue && fixture.Manifest.Pathological.Value != isPathological)
            {
                result.Outcome = FixtureOutcome.Failed;
                result.Reason =
                    $"manifest error: pathological={fixture.Manifest.Pathological.Value.ToString().ToLowerInvariant()} "
                    + $"disagrees with category '{fixture.Manifest.Category}'";
                report.Results.Add(result);
                continue;
            }

            if (isPathological && !includePathological)
            {
                report.ExcludedPathologicalCount++;
                continue;
            }

            // manifest.requires must mechanically match what grammar.xml actually contains (see
            // PROTOCOL.md section 5 / RequiresDerivation) -- a hand-authored drift here is a
            // manifest bug, caught the same way the pathological/category mismatch above is.
            List<string> derivedRequires = RequiresDerivation.Derive(fixture.GrammarPath);
            if (
                !new HashSet<string>(derivedRequires, StringComparer.Ordinal).SetEquals(
                    new HashSet<string>(fixture.Manifest.Requires, StringComparer.Ordinal)
                )
            )
            {
                result.Outcome = FixtureOutcome.Failed;
                result.Reason =
                    $"manifest error: requires [{string.Join(",", fixture.Manifest.Requires)}] does not match "
                    + $"grammar-derived [{string.Join(",", derivedRequires)}]";
                report.Results.Add(result);
                continue;
            }

            List<string> missingCapabilities = fixture
                .Manifest.Requires.Where(req => !engine.Capabilities.Contains(req))
                .ToList();
            if (missingCapabilities.Count > 0)
            {
                result.Outcome = FixtureOutcome.Skipped;
                result.Reason =
                    $"requires [{string.Join(",", fixture.Manifest.Requires)}], "
                    + $"engine declares [{string.Join(",", engine.Capabilities.OrderBy(c => c))}]; "
                    + $"missing [{string.Join(",", missingCapabilities)}]";
                report.Results.Add(result);
                continue;
            }

            // Read once, before the try: the old code re-read expected.tsv from inside the catch
            // block too, so a genuinely missing file could throw from inside exception handling.
            List<TsvRow> expected = SignatureTsv.ReadFile(fixture.ExpectedPath);

            var sw = Stopwatch.StartNew();
            try
            {
                List<TsvRow> actual = RunWithBudget(engine, fixture);
                sw.Stop();
                result.ElapsedMs = sw.ElapsedMilliseconds;

                // A handful of fixtures deliberately pin a CRASH as their ground truth (e.g.
                // rewrite/simultaneous-epenthesis-cascade's InfiniteLoopException): the manifest
                // says so explicitly (expectCrash: true) rather than the harness inferring it from
                // an empty expected.tsv. If the engine returns normally here, it did not reproduce
                // that crash, which is itself a conformance failure.
                if (fixture.Manifest.ExpectCrash)
                {
                    result.Outcome = FixtureOutcome.Failed;
                    result.Reason = "expected crash, engine produced output";
                }
                else
                {
                    FixtureDiff diff = Diff.Compare(expected, actual);
                    result.WordResults = diff.WordResults;

                    // Enforced whenever a budget is present, regardless of category: today only
                    // pathological fixtures carry one, but nothing about the check is category-
                    // specific.
                    bool overBudget =
                        fixture.Manifest.Budget != null && result.ElapsedMs > fixture.Manifest.Budget.WallClockMs;

                    if (overBudget)
                    {
                        result.Outcome = FixtureOutcome.Failed;
                        result.Reason =
                            $"exceeded wall-clock budget ({result.ElapsedMs}ms > {fixture.Manifest.Budget.WallClockMs}ms)";
                    }
                    else if (diff.AllPassed)
                    {
                        result.Outcome = FixtureOutcome.Passed;
                    }
                    else
                    {
                        result.Outcome = FixtureOutcome.Failed;
                        // Name the words. A bare count cannot be diagnosed from a CI log on a machine you cannot reach.

                        WordResult[] mismatched = diff.WordResults.Where(word => !word.Passed).ToArray();

                        string named = string.Join(
                            "; ",
                            mismatched.Take(5).Select(word => $"{word.Word}: {Truncate(word.Detail)}")
                        );

                        result.Reason =
                            mismatched.Length > 5
                                ? $"{mismatched.Length}/{
diff.WordResults
.Count} word(s) mismatched: {named}; and {mismatched.Length - 5} more"
                                : $"{mismatched.Length}/{
diff.WordResults
.Count} word(s) mismatched: {named}";
                    }
                }
            }
            catch (EngineCrashException ex)
            {
                sw.Stop();
                result.ElapsedMs = sw.ElapsedMilliseconds;

                if (fixture.Manifest.ExpectCrash)
                {
                    result.Outcome = FixtureOutcome.Passed;
                    result.Reason = $"engine crashed as expected (manifest expectCrash: true): {ex.Message}";
                }
                else
                {
                    result.Outcome = FixtureOutcome.Failed;
                    result.Reason = $"engine crashed: {ex.Message}";
                }
            }
            catch (TimeoutException ex) when (fixture.Manifest.Budget != null)
            {
                sw.Stop();
                result.ElapsedMs = sw.ElapsedMilliseconds;

                result.Outcome = FixtureOutcome.Failed;
                result.Reason = $"exceeded budget {fixture.Manifest.Budget.WallClockMs}ms: {ex.Message}";
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.ElapsedMs = sw.ElapsedMilliseconds;

                // Any other exception is a plain harness error (bad --adapter template, process
                // couldn't start, timed out, etc.) -- never a pass, regardless of expectCrash.
                result.Outcome = FixtureOutcome.Failed;
                result.Reason = $"harness error: {ex.Message}";
            }

            report.Results.Add(result);
        }

        return report;
    }

    /// <summary>
    /// Runs the engine for one fixture. If the manifest carries a budget, engine.Run happens on a
    /// background thread and this waits up to budget + a small margin; AdapterEngine independently
    /// plumbs the same budget into its own process timeout (so a subprocess adapter gets killed),
    /// but self-check's in-process call has no external process to kill, so this watchdog is what
    /// keeps a runaway self-check parse from hanging the whole harness run. If the watchdog expires,
    /// a TimeoutException is thrown and the background thread is simply abandoned -- this is a
    /// short-lived CLI, so a leaked runaway thread on the rare budget-blown fixture is an acceptable
    /// trade for keeping this shape simple.
    /// </summary>
    private static List<TsvRow> RunWithBudget(IEngine engine, MaterializedFixture fixture)
    {
        FixtureBudget budget = fixture.Manifest.Budget;
        if (budget == null)
            return engine.Run(fixture);

        Task<List<TsvRow>> task = Task.Run(() => engine.Run(fixture));
        long marginMs = Math.Max(budget.WallClockMs / 5, 1000);
        bool completed;
        try
        {
            completed = task.Wait((int)(budget.WallClockMs + marginMs));
        }
        catch (AggregateException ex) when (ex.InnerException != null)
        {
            // Unwrap: rethrow the engine's own exception so the caller's EngineCrashException/
            // generic catch blocks see the real exception type, not Task's wrapper.
            throw ex.InnerException;
        }
        if (!completed)
            throw new TimeoutException("watchdog timed out waiting for engine.Run");
        return task.GetAwaiter().GetResult();
    }

    /// <summary>Shortens a per-word detail so a fixture reason stays readable in a log.</summary>
    /// <param name="detail">The word detail, which may carry every expected and actual parse.</param>
    private static string Truncate(string detail)
    {
        const int Limit = 160;
        if (string.IsNullOrEmpty(detail) || detail.Length <= Limit)
        {
            return detail;
        }

        return detail[..Limit] + "...";
    }
}
