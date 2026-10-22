using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

public class RunReport
{
    public List<FixtureResult> Results { get; } = new();
    public int FixtureCount { get; set; }
    public int ExcludedPathologicalCount { get; set; }

    public int Passed => Results.Count(r => r.Outcome == FixtureOutcome.Passed);
    public int Failed => Results.Count(r => r.Outcome == FixtureOutcome.Failed);
    public int Skipped => Results.Count(r => r.Outcome == FixtureOutcome.Skipped);
}

/// <summary>
/// Runs fixtures under conformance/languages and conformance/edge-cases. Self-check mode is a
/// custom, in-process loop (it needs per-word signature-SET comparison against words.yaml and
/// traced-rules verification, neither of which <see cref="MaterializedRunner"/>/<see cref="SelfCheckEngine"/>
/// does). Adapter mode instead materializes each fixture onto disk and hands it to
/// <see cref="MaterializedRunner"/> + <see cref="AdapterEngine"/> -- see <see cref="FixtureMaterializer"/>.
/// </summary>
public static class Runner
{
    public static RunReport RunSelfCheck(
        List<Fixture> fixtures,
        bool includePathological,
        IReadOnlySet<string> engineCapabilities,
        bool propose,
        TextWriter proposeOutput
    )
    {
        var report = new RunReport();
        report.FixtureCount = fixtures.Count;
        foreach (Fixture fixture in fixtures)
        {
            foreach (WordRun run in fixture.Words.Runs)
            {
                FixtureResult result = RunOneSelfCheck(
                    fixture,
                    run,
                    includePathological,
                    engineCapabilities,
                    propose,
                    proposeOutput
                );
                if (result == null)
                    report.ExcludedPathologicalCount++;
                else
                    report.Results.Add(result);
            }
        }
        return report;
    }

    private static FixtureResult RunOneSelfCheck(
        Fixture fixture,
        WordRun run,
        bool includePathological,
        IReadOnlySet<string> engineCapabilities,
        bool propose,
        TextWriter proposeOutput
    )
    {
        string runId = run.Name == "default" ? fixture.Id : $"{fixture.Id} [variant {run.Name}]";
        var result = new FixtureResult { FixtureId = runId };

        List<string> derivedRequires = RequiresDerivation.Derive(
            fixture.GrammarPath,
            fixture.Words.Runs.Select(r => r.Morpher)
        );
        if (
            !new HashSet<string>(derivedRequires, StringComparer.Ordinal).SetEquals(
                new HashSet<string>(fixture.Words.Requires, StringComparer.Ordinal)
            )
        )
        {
            result.Outcome = FixtureOutcome.Failed;
            result.Reason =
                $"words.yaml error: requires [{string.Join(",", fixture.Words.Requires)}] does not match "
                + $"grammar-derived [{string.Join(",", derivedRequires)}]";
            return result;
        }

        bool isPathological = fixture.Words.BudgetMs.HasValue;
        if (isPathological && !includePathological)
            return null;

        List<string> missingCapabilities = fixture.Words.Requires.Where(r => !engineCapabilities.Contains(r)).ToList();
        if (missingCapabilities.Count > 0)
        {
            result.Outcome = FixtureOutcome.Skipped;
            result.Reason =
                $"requires [{string.Join(",", fixture.Words.Requires)}], missing [{string.Join(",", missingCapabilities)}]";
            return result;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            RunWithBudget(fixture, run, result, propose, proposeOutput);
            sw.Stop();
            result.ElapsedMs = sw.ElapsedMilliseconds;

            if (fixture.Words.ExpectCrash)
            {
                result.Outcome = FixtureOutcome.Failed;
                result.Reason = "expected crash, engine produced output";
            }
            else
            {
                bool overBudget = fixture.Words.BudgetMs.HasValue && result.ElapsedMs > fixture.Words.BudgetMs.Value;
                if (overBudget)
                {
                    result.Outcome = FixtureOutcome.Failed;
                    result.Reason = $"exceeded wall-clock budget ({result.ElapsedMs}ms > {fixture.Words.BudgetMs}ms)";
                }
                else if (result.WordResults.All(w => w.Passed))
                {
                    result.Outcome = FixtureOutcome.Passed;
                }
                else
                {
                    result.Outcome = FixtureOutcome.Failed;
                    // Name the words. A bare count cannot be diagnosed from a CI log on a machine you cannot reach.

                    WordResult[] mismatched = result.WordResults.Where(word => !word.Passed).ToArray();

                    string named = string.Join(
                        "; ",
                        mismatched.Take(5).Select(word => $"{word.Word}: {Truncate(word.Detail)}")
                    );

                    result.Reason =
                        mismatched.Length > 5
                            ? $"{mismatched.Length}/{
result.WordResults
.Count} word(s) mismatched: {named}; and {mismatched.Length - 5} more"
                            : $"{mismatched.Length}/{
result.WordResults
.Count} word(s) mismatched: {named}";
                }
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.ElapsedMs = sw.ElapsedMilliseconds;
            // The watchdog's own timeout is never a crash match, as in adapter mode.
            if (fixture.Words.ExpectCrash && ex is not TimeoutException)
            {
                result.Outcome = FixtureOutcome.Passed;
                result.Reason = $"engine crashed as expected (words.yaml expect_crash: true): {ex.Message}";
            }
            else
            {
                result.Outcome = FixtureOutcome.Failed;
                result.Reason = $"engine crashed in run '{run.Name}': {ex.Message}";
            }
        }

        return result;
    }

    // Mirrors MaterializedRunner.RunWithBudget's watchdog shape (see that method's doc comment) -- a
    // budgeted fixture gets the same "background thread + Task.Wait" timeout enforcement.
    private static void RunWithBudget(
        Fixture fixture,
        WordRun run,
        FixtureResult result,
        bool propose,
        TextWriter proposeOutput
    )
    {
        if (!fixture.Words.BudgetMs.HasValue)
        {
            RunAllWords(fixture, run, result.WordResults, propose, proposeOutput, CancellationToken.None);
            return;
        }

        long budgetMs = fixture.Words.BudgetMs.Value;
        // The task fills its own list, so a task abandoned on timeout cannot mutate the reported result.
        var wordResults = new List<WordResult>();
        using var cancellation = new CancellationTokenSource();
        Task task = Task.Run(() => RunAllWords(fixture, run, wordResults, propose, proposeOutput, cancellation.Token));
        long marginMs = Math.Max(budgetMs / 5, 1000);
        bool completed;
        try
        {
            completed = task.Wait((int)(budgetMs + marginMs));
        }
        catch (AggregateException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
        if (!completed)
        {
            cancellation.Cancel();
            throw new TimeoutException("watchdog timed out waiting for self-check engine");
        }
        result.WordResults.AddRange(wordResults);
    }

    private static void RunAllWords(
        Fixture fixture,
        WordRun run,
        List<WordResult> wordResults,
        bool propose,
        TextWriter proposeOutput,
        CancellationToken cancellationToken
    )
    {
        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);
        Morpher morpher = ConformanceMorpherFactory.CreateTracing(language, run.Morpher);
        GrammarRuleIndex ruleIndex = GrammarRuleIndex.Load(fixture.GrammarPath);

        foreach (WordEntry word in run.Words)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool guessRoot = run.Morpher.GuessRootsFor(word);
            List<Word> results;
            object trace = null;
            bool wasSkipped = false;
            try
            {
                results = morpher.ParseWord(word.Word, out trace, guessRoot).ToList();
            }
            catch (InvalidShapeException)
            {
                wasSkipped = true;
                results = new List<Word>();
            }

            string actualSignature = SignatureFormat.BuildSignature(results);
            string expectedSignature = FixtureMaterializer.BuildExpectedSignature(word);
            List<string> actualEntries = SignatureTsv.SplitSignature(actualSignature);
            List<string> expectedEntries = SignatureTsv.SplitSignature(expectedSignature);

            bool signatureMatches = actualEntries.SequenceEqual(expectedEntries, StringComparer.Ordinal);
            if (!signatureMatches)
            {
                wordResults.Add(
                    new WordResult
                    {
                        Word = word.Word,
                        Passed = false,
                        Detail =
                            $"expected [{string.Join(" | ", expectedEntries)}] got [{string.Join(" | ", actualEntries)}]",
                    }
                );
                if (propose && run.Name == "default")
                {
                    ProposePatchWriter.WriteSignatureProposal(proposeOutput, fixture, word, actualEntries);
                }
                else if (propose)
                {
                    proposeOutput.WriteLine(
                        $"# --propose cannot safely patch variant '{run.Name}' in {fixture.Id}; edit that variant's complete words list by hand."
                    );
                }
                continue;
            }

            if (word.ExpectSkip)
            {
                wordResults.Add(
                    new WordResult
                    {
                        Word = word.Word,
                        Passed = wasSkipped,
                        Detail = wasSkipped
                            ? "ok (skipped: InvalidShapeException)"
                            : "declared expect_skip but engine produced a zero-parse 'ok' without skipping",
                    }
                );
                continue;
            }

            if (word.ExpectFail)
            {
                wordResults.Add(
                    new WordResult
                    {
                        Word = word.Word,
                        Passed = !wasSkipped,
                        Detail = !wasSkipped
                            ? "ok (0 parses)"
                            : "declared expect_fail (status ok) but engine SKIPPED it (InvalidShapeException) -- use expect_skip",
                    }
                );
                continue;
            }

            Dictionary<string, List<Word>> actualBySignature = results
                .GroupBy(w => SignatureFormat.BuildSignature(new[] { w }))
                .ToDictionary(g => g.Key, g => g.ToList());
            HashSet<string> wordLevelRuleIds = TraceRuleAttributor.WordLevelRuleIds(trace, ruleIndex);

            bool wordRulesOk = true;
            var ruleDetails = new List<string>();
            foreach (ParseEntry parse in word.Parses)
            {
                if (!actualBySignature.TryGetValue(parse.Signature, out List<Word> matches) || matches.Count == 0)
                    continue; // can't happen given signatureMatches, but stay defensive
                foreach (Word match in matches)
                {
                    HashSet<string> actualRuleIds = TraceRuleAttributor.MorphologicalRuleIds(match, ruleIndex);
                    actualRuleIds.UnionWith(wordLevelRuleIds);
                    var declaredRuleIds = new HashSet<string>(parse.Rules, StringComparer.Ordinal);
                    if (!actualRuleIds.SetEquals(declaredRuleIds))
                    {
                        wordRulesOk = false;
                        ruleDetails.Add(
                            $"parse '{parse.Signature}': declared rules [{string.Join(",", parse.Rules)}] "
                                + $"!= traced [{string.Join(",", actualRuleIds.OrderBy(x => x, StringComparer.Ordinal))}]"
                        );
                    }
                }
            }

            wordResults.Add(
                new WordResult
                {
                    Word = word.Word,
                    Passed = wordRulesOk,
                    Detail = wordRulesOk ? "ok" : string.Join("; ", ruleDetails),
                }
            );
        }
    }

    // ---- Adapter mode: materialize onto disk + reuse MaterializedRunner/AdapterEngine, unmodified. ----

    public static RunReport RunAdapter(List<Fixture> fixtures, IEngine engine, bool includePathological)
    {
        var report = new RunReport();
        report.FixtureCount = fixtures.Count;
        // One call over every run of every fixture, so an expect_crash fixture has an ordinary fixture to use
        // as its control.
        var materialized = new List<MaterializedFixture>();
        try
        {
            foreach (Fixture fixture in fixtures)
            {
                foreach (WordRun run in fixture.Words.Runs)
                {
                    try
                    {
                        materialized.Add(FixtureMaterializer.Materialize(fixture, run));
                    }
                    catch (NamedVariantHasNoAdapterVisibleWordsException ex)
                    {
                        report.Results.Add(
                            new FixtureResult
                            {
                                FixtureId = run.Name == "default" ? fixture.Id : $"{fixture.Id} [variant {run.Name}]",
                                Outcome = FixtureOutcome.Failed,
                                Reason = ex.Message,
                            }
                        );
                    }
                }
            }

            MaterializedRunReport runReport = MaterializedRunner.Run(materialized, engine, includePathological);
            report.ExcludedPathologicalCount = runReport.ExcludedPathologicalCount;
            report.Results.AddRange(runReport.Results);
        }
        finally
        {
            foreach (MaterializedFixture fixture in materialized)
                FixtureMaterializer.Cleanup(fixture);
        }
        return report;
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
