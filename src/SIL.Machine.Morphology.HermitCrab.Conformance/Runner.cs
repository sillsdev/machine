using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

public class RunReport
{
    public List<FixtureResult> Results { get; } = new();
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
        foreach (Fixture fixture in fixtures)
        {
            FixtureResult result = RunOneSelfCheck(
                fixture,
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
        return report;
    }

    private static FixtureResult RunOneSelfCheck(
        Fixture fixture,
        bool includePathological,
        IReadOnlySet<string> engineCapabilities,
        bool propose,
        TextWriter proposeOutput
    )
    {
        var result = new FixtureResult { FixtureId = fixture.Id };

        // Mechanical requires-check against the words.yaml front matter (see PROTOCOL.md section 5 /
        // RequiresDerivation) -- a hand-authored drift here is a words.yaml bug.
        List<string> derivedRequires = RequiresDerivation.Derive(fixture.GrammarPath);
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
            RunWithBudget(fixture, result, propose, proposeOutput);
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
            if (fixture.Words.ExpectCrash)
            {
                result.Outcome = FixtureOutcome.Passed;
                result.Reason = $"engine crashed as expected (words.yaml expect_crash: true): {ex.Message}";
            }
            else
            {
                result.Outcome = FixtureOutcome.Failed;
                result.Reason = $"engine crashed: {ex.Message}";
            }
        }

        return result;
    }

    // Mirrors MaterializedRunner.RunWithBudget's watchdog shape (see that method's doc comment) -- a
    // budgeted fixture gets the same "background thread + Task.Wait" timeout enforcement.
    private static void RunWithBudget(Fixture fixture, FixtureResult result, bool propose, TextWriter proposeOutput)
    {
        if (!fixture.Words.BudgetMs.HasValue)
        {
            RunAllWords(fixture, result, propose, proposeOutput);
            return;
        }

        long budgetMs = fixture.Words.BudgetMs.Value;
        Task task = Task.Run(() => RunAllWords(fixture, result, propose, proposeOutput));
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
            throw new TimeoutException("watchdog timed out waiting for self-check engine");
    }

    private static void RunAllWords(Fixture fixture, FixtureResult result, bool propose, TextWriter proposeOutput)
    {
        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);
        var traceManager = new TraceManager { IsTracing = true };
        var morpher = new Morpher(traceManager, language);
        GrammarRuleIndex ruleIndex = GrammarRuleIndex.Load(fixture.GrammarPath);

        foreach (WordEntry word in fixture.Words.Words)
        {
            bool guessRoot = word.Parses.Any(p => p.Guess);
            List<Word> results;
            object trace;
            bool wasSkipped = false;
            try
            {
                results = morpher.ParseWord(word.Word, out trace, guessRoot).ToList();
            }
            catch (InvalidShapeException)
            {
                // The engine SKIPS this word (an undeclared segment, etc.) rather than returning a
                // zero-parse "ok" -- the adapter contract's "SKIPPED" status. Distinguished from a
                // genuine 0-parse below so expect_fail (ok) and expect_skip (SKIPPED) each verify the
                // status the materialized expected.tsv (and any real adapter) will actually diff on.
                wasSkipped = true;
                results = new List<Word>();
                trace = null;
            }

            string actualSignature = SignatureFormat.BuildSignature(results);
            string expectedSignature = FixtureMaterializer.BuildExpectedSignature(word);
            List<string> actualEntries = SignatureTsv.SplitSignature(actualSignature);
            List<string> expectedEntries = SignatureTsv.SplitSignature(expectedSignature);

            bool signatureMatches = actualEntries.SequenceEqual(expectedEntries, StringComparer.Ordinal);
            if (!signatureMatches)
            {
                result.WordResults.Add(
                    new WordResult
                    {
                        Word = word.Word,
                        Passed = false,
                        Detail =
                            $"expected [{string.Join(" | ", expectedEntries)}] got [{string.Join(" | ", actualEntries)}]",
                    }
                );
                if (propose)
                    ProposePatchWriter.WriteSignatureProposal(proposeOutput, fixture, word, actualEntries);
                continue;
            }

            if (word.ExpectSkip)
            {
                // Declared SKIPPED: PASS iff the engine actually threw InvalidShapeException. A word
                // that instead produced a genuine empty parse set (status "ok") is mis-declared -- it
                // would diff as ok-vs-SKIPPED against a real adapter, so fail it here too.
                result.WordResults.Add(
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
                // Declared genuine zero-parse "ok": PASS iff the engine did NOT skip. A word that was
                // actually SKIPPED (InvalidShapeException) is mis-declared and should be expect_skip --
                // it would diff as SKIPPED-vs-ok against a real adapter (adapter mode materializes "ok"
                // for expect_fail). Keeping self-check strict here keeps the two modes consistent.
                result.WordResults.Add(
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

            // Signature set matched; now verify the traced "rules:" list per parse. Group actual
            // results by their own individual signature so an ambiguous word's N declared parses
            // can each be matched to the one result that produced it (signatureMatches above already
            // proved this is a 1:1 correspondence at the SET level; duplicate signature strings
            // across parses are not expected and, if they occurred, would just re-check the same
            // result against every parse sharing that signature).
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

            result.WordResults.Add(
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
        foreach (Fixture fixture in fixtures)
        {
            MaterializedFixture materialized = FixtureMaterializer.Materialize(fixture);
            try
            {
                MaterializedRunReport oneReport = MaterializedRunner.Run(
                    new List<MaterializedFixture> { materialized },
                    engine,
                    includePathological
                );
                report.ExcludedPathologicalCount += oneReport.ExcludedPathologicalCount;
                foreach (FixtureResult r in oneReport.Results)
                {
                    r.FixtureId = fixture.Id; // already fixture.Id (came from the synthetic manifest); kept explicit for clarity
                    report.Results.Add(r);
                }
            }
            finally
            {
                FixtureMaterializer.Cleanup(materialized);
            }
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
