using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

internal class Program
{
    private static int Main(string[] args)
    {
        string fixturesRoot = null;
        string adapterTemplate = null;
        string capabilitiesArg = null;
        bool capabilitiesProvided = false;
        bool includePathological = false;
        bool coverageReport = false;
        string constructsPath = null;
        bool propose = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--fixtures":
                    if (!TryGetNextArg(args, ref i, "--fixtures", out fixturesRoot))
                        return 2;
                    break;
                case "--adapter":
                    if (!TryGetNextArg(args, ref i, "--adapter", out adapterTemplate))
                        return 2;
                    break;
                case "--capabilities":
                    if (!TryGetNextArg(args, ref i, "--capabilities", out capabilitiesArg))
                        return 2;
                    capabilitiesProvided = true;
                    break;
                case "--include-pathological":
                    includePathological = true;
                    break;
                case "--coverage-report":
                    coverageReport = true;
                    break;
                case "--constructs":
                    if (!TryGetNextArg(args, ref i, "--constructs", out constructsPath))
                        return 2;
                    break;
                case "--propose":
                    propose = true;
                    break;
                case "-h":
                case "--help":
                    PrintUsage();
                    return 0;
                default:
                    Console.Error.WriteLine($"unrecognized argument: {args[i]}");
                    PrintUsage();
                    return 2;
            }
        }

        if (string.IsNullOrEmpty(fixturesRoot))
        {
            Console.Error.WriteLine("--fixtures <path> is required");
            PrintUsage();
            return 2;
        }

        List<Fixture> fixtures;
        try
        {
            fixtures = Fixture.DiscoverAll(fixturesRoot);
        }
        catch (WordsYamlException ex)
        {
            Console.Error.WriteLine($"words.yaml error: {ex.Message}");
            return 2;
        }
        Console.WriteLine($"discovered {fixtures.Count} fixture(s) under '{fixturesRoot}'");

        if (fixtures.Count == 0)
        {
            Console.Error.WriteLine($"no fixtures discovered under '{fixturesRoot}'");
            return 2;
        }

        if (coverageReport)
        {
            constructsPath ??= Path.Combine(fixturesRoot, "constructs.txt");
            // A dead rule (a grammar.xml rule id no word ever exercises) or a label-only attribution
            // outside the frozen baseline (a NEW citation-without-evidence hole) are both authoring
            // defects, not merely informational, so --coverage-report fails the same way self-check
            // already fails on Failed > 0: exit code reflects a suite that isn't clean, with no
            // separate flag to remember to pass in CI.
            bool anyGateFailure = RunCoverageReport(fixtures, fixturesRoot, constructsPath);
            return anyGateFailure ? 1 : 0;
        }

        IEngine engine;
        if (adapterTemplate != null)
        {
            HashSet<string> capabilities = ParseCapabilities(capabilitiesArg ?? "");
            engine = new AdapterEngine(adapterTemplate, capabilities);
        }
        else
        {
            // Self-check mode implies phonology support (it IS the reference engine) unless the
            // caller explicitly overrides --capabilities, which is how the capability-filtering
            // mechanism itself gets exercised without needing a second engine (see conformance
            // framework verification notes).
            IReadOnlySet<string> capabilities = capabilitiesProvided ? ParseCapabilities(capabilitiesArg) : null;
            engine = new SelfCheckEngine(capabilities);
        }

        RunReport report =
            adapterTemplate != null
                ? Runner.RunAdapter(fixtures, engine, includePathological)
                : Runner.RunSelfCheck(fixtures, includePathological, engine.Capabilities, propose, Console.Out);
        PrintRunReport(report, engine);

        bool anyRan = report.Passed > 0 || report.Failed > 0;
        if (!anyRan)
        {
            Console.Error.WriteLine("no fixtures actually ran (all excluded or skipped) -- treating this as an error");
            return 2;
        }

        return report.Failed > 0 ? 1 : 0;
    }

    /// <summary>Returns the argument following <paramref name="flag"/>, or prints usage and returns
    /// false if <paramref name="flag"/> was the last token (no bounds-check crash on a truncated
    /// command line).</summary>
    private static bool TryGetNextArg(string[] args, ref int i, string flag, out string value)
    {
        if (i + 1 >= args.Length)
        {
            Console.Error.WriteLine($"{flag} requires a value");
            PrintUsage();
            value = null;
            return false;
        }
        value = args[++i];
        return true;
    }

    private static HashSet<string> ParseCapabilities(string arg)
    {
        return arg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static void PrintRunReport(RunReport report, IEngine engine)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"engine: {engine.Name}, capabilities: [{string.Join(",", engine.Capabilities.OrderBy(c => c))}]"
        );
        if (report.ExcludedPathologicalCount > 0)
        {
            Console.WriteLine(
                $"{report.ExcludedPathologicalCount} pathological (budget_ms) fixture(s) excluded by default (--include-pathological to run)"
            );
        }
        Console.WriteLine();

        foreach (FixtureResult result in report.Results)
        {
            string status = result.Outcome switch
            {
                FixtureOutcome.Passed => "PASS",
                FixtureOutcome.Failed => "FAIL",
                FixtureOutcome.Skipped => "SKIP",
                _ => "?",
            };
            Console.WriteLine($"[{status}] {result.FixtureId} ({result.ElapsedMs}ms) {result.Reason}");
            if (result.Outcome == FixtureOutcome.Failed)
            {
                foreach (WordResult w in result.WordResults.Where(w => !w.Passed))
                    Console.WriteLine($"    word '{w.Word}': {w.Detail}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(
            $"totals: {report.Passed} passed, {report.Failed} failed, {report.Skipped} skipped (of {report.Results.Count} attempted)"
        );
    }

    // "Tracing" is the one construct the suite deliberately never covers (it was never in
    // expected.tsv's domain) -- see docs/conformance-language-suite-plan.md sections 3 and 7.
    private const string OutOfScopeConstruct = "Tracing (TraceType)";

    /// <summary>
    /// A NAMED, frozen allowlist of every (fixture, rule id) pair permitted to stay label-only --
    /// not a count, because a count lets one rule be silently swapped for another while the number
    /// stays the same. Investigated 2026-08-10: <c>aKanta</c> is a lexical entry's own allomorph
    /// (an <c>AllomorphCoOccurrenceRule</c> pseudo-id, see <see cref="GrammarRuleIndex"/>'s own doc
    /// comment), and the runtime <see cref="Allomorph"/> object never retains its grammar.xml
    /// <c>id</c> attribute at all (unlike <see cref="Morpheme.Id"/>, which every other rule kind
    /// resolves through) -- so no trace can currently name WHICH of a free-variation morpheme's
    /// several allomorphs failed. Closing this needs a new engine capability (an
    /// <c>Allomorph.Id</c> passthrough from the loader), not a conformance-tool fix; see
    /// <see cref="FailureRuleAttributor"/>'s own doc comment for the full investigation. Any entry
    /// that appears in <see cref="CoverageReport.CoverageResult.LabelOnlyRules"/> but NOT here is a
    /// NEW hole this baseline exists to catch -- the gate must fail on it, unconditionally.
    /// </summary>
    private static readonly HashSet<(string FixtureId, string RuleId)> LabelOnlyBaseline = new()
    {
        ("languages/suffixing-evidential-adjacency-chain", "aKanta"),
    };

    /// <summary>Writes coverage.csv/rules.csv/fixtures.csv and prints the report. Returns true if
    /// any dead rule was found, or any label-only attribution outside <see cref="LabelOnlyBaseline"/>
    /// was found, either of which the caller turns into a non-zero exit code.</summary>
    private static bool RunCoverageReport(List<Fixture> fixtures, string fixturesRoot, string constructsPath)
    {
        string coverageCsvPath = Path.Combine(fixturesRoot, "coverage.csv");
        string rulesCsvPath = Path.Combine(fixturesRoot, "rules.csv");
        string fixtureIndexCsvPath = Path.Combine(fixturesRoot, "fixtures.csv");
        CoverageReport.CoverageResult result = CoverageReport.WriteCsvs(
            fixtures,
            coverageCsvPath,
            rulesCsvPath,
            fixtureIndexCsvPath
        );

        Console.WriteLine();
        Console.WriteLine("coverage report");
        Console.WriteLine("===============");
        Console.WriteLine($"wrote {coverageCsvPath}");
        Console.WriteLine($"wrote {rulesCsvPath}");
        Console.WriteLine($"wrote {fixtureIndexCsvPath}");

        // Absolute construct-coverage check against constructs.txt: every construct except Tracing
        // must be covered.
        if (File.Exists(constructsPath))
        {
            List<string> checklist = CoverageReport.LoadConstructChecklist(constructsPath);
            List<string> uncovered = checklist
                .Where(c => !string.Equals(c, OutOfScopeConstruct, StringComparison.Ordinal))
                .Where(c => !result.CoveredConstructs.Contains(c))
                .ToList();
            Console.WriteLine();
            int inScope = checklist.Count(c => !string.Equals(c, OutOfScopeConstruct, StringComparison.Ordinal));
            Console.WriteLine(
                $"construct coverage: {inScope - uncovered.Count}/{inScope} in-scope constructs covered "
                    + $"(Tracing out of scope by design)"
            );
            if (uncovered.Count > 0)
            {
                Console.WriteLine($"*** {uncovered.Count} CONSTRUCT(S) AT ZERO COVERAGE ***");
                foreach (string c in uncovered)
                    Console.WriteLine($"  {c}");
            }
        }

        if (result.DeadRules.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"*** {result.DeadRules.Count} DEAD RULE(S) (exercised by zero words) ***");
            foreach (CoverageReport.DeadRule dead in result.DeadRules)
                Console.WriteLine($"  {dead.FixtureId}: rule '{dead.RuleId}'");
        }
        else
        {
            Console.WriteLine("0 dead rules across all grammars.");
        }

        // A label-only rule has attribution (so it isn't dead), but every one of them is printed
        // prominently regardless: exercised ONLY by an unverified blocked_by label is the exact
        // citation-without-evidence shortcut the dead-rule gate must not let go invisible. See
        // CoverageReport's class doc comment. Gated separately from dead-rule status: any entry NOT
        // in the frozen LabelOnlyBaseline is a NEW hole, and the gate fails on it -- see that
        // field's own doc comment for why a count would not catch a rule silently swapped in place
        // of another.
        bool anyNewLabelOnly = false;
        if (result.LabelOnlyRules.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"*** {result.LabelOnlyRules.Count} LABEL-ONLY ATTRIBUTION(S) "
                    + "(exercised only by an unverified 'blocked_by' label, never a verified rules:/crash trace) ***"
            );
            foreach (CoverageReport.LabelOnlyRule labelOnly in result.LabelOnlyRules)
            {
                bool baselined = LabelOnlyBaseline.Contains((labelOnly.FixtureId, labelOnly.RuleId));
                Console.WriteLine(
                    $"  {labelOnly.FixtureId}: rule '{labelOnly.RuleId}'"
                        + (baselined ? " (frozen baseline, permitted)" : " *** NOT IN BASELINE ***")
                );
                if (!baselined)
                    anyNewLabelOnly = true;
            }
            if (anyNewLabelOnly)
            {
                Console.WriteLine(
                    "*** at least one label-only attribution above is NOT in the frozen baseline -- "
                        + "this is a NEW hole, not the known one, and fails this gate. ***"
                );
            }
        }
        else
        {
            Console.WriteLine("0 label-only attributions across all grammars.");
        }

        return result.DeadRules.Count > 0 || anyNewLabelOnly;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            usage: hc-conformance --fixtures <path> [options]

            options:
              --adapter "<command template>"   Run fixtures through an external engine adapter.
                                                Template placeholders: {grammar} {words} {output}.
                                                Omit to run the harness's self-check mode (in-process
                                                C# oracle) instead.
              --capabilities <comma-list>      Capability set the engine under test declares (e.g.
                                                "phonology", or "" for none). Defaults to the full set
                                                in self-check mode, empty in --adapter mode.
              --include-pathological           Also run category:pathological fixtures (excluded by
                                                default).
              --coverage-report                Print a coverage report instead of running fixtures.
              --constructs <path>               Construct checklist file for --coverage-report
                                                (default: <fixtures>/constructs.txt).
              --propose                        Self-check only: on a signature mismatch, print
                                                the words.yaml patch that would reconcile it. Never
                                                writes any file.
            """
        );
    }
}
