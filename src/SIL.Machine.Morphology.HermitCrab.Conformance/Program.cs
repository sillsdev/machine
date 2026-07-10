using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.Machine.Morphology.HermitCrab.Conformance.V2;

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

        // v1 fixtures (manifest.json-keyed) and v2 fixtures (languages/, edge-cases/, words.yaml-keyed)
        // are discovered independently and never overlap on disk -- see FixtureV2.DiscoverAll's doc
        // comment. Both run in the same invocation so a single `--fixtures conformance` run covers
        // the whole suite during the v1->v2 migration window (docs/conformance-language-suite-plan.md
        // phase G1-G4).
        List<Fixture> fixtures = Fixture.DiscoverAll(fixturesRoot);
        List<FixtureV2> fixturesV2;
        try
        {
            fixturesV2 = FixtureV2.DiscoverAll(fixturesRoot);
        }
        catch (WordsYamlException ex)
        {
            Console.Error.WriteLine($"words.yaml error: {ex.Message}");
            return 2;
        }
        Console.WriteLine(
            $"discovered {fixtures.Count} v1 fixture(s) and {fixturesV2.Count} v2 fixture(s) under '{fixturesRoot}'"
        );

        if (fixtures.Count == 0 && fixturesV2.Count == 0)
        {
            Console.Error.WriteLine($"no fixtures discovered under '{fixturesRoot}'");
            return 2;
        }

        if (coverageReport)
        {
            int rc = 0;
            if (fixtures.Count > 0)
            {
                constructsPath ??= Path.Combine(fixturesRoot, "constructs.txt");
                rc = RunCoverageReport(fixtures, constructsPath);
            }
            if (fixturesV2.Count > 0)
            {
                constructsPath ??= Path.Combine(fixturesRoot, "constructs.txt");
                RunCoverageReportV2(fixturesV2, fixturesRoot, constructsPath);
            }
            return rc;
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

        bool anyRan = false;
        bool anyFailed = false;

        if (fixtures.Count > 0)
        {
            RunReport report = Runner.Run(fixtures, engine, includePathological);
            PrintRunReport(report, engine);
            anyRan |= report.Passed > 0 || report.Failed > 0;
            anyFailed |= report.Failed > 0;
        }

        if (fixturesV2.Count > 0)
        {
            RunnerV2.RunReportV2 reportV2 =
                adapterTemplate != null
                    ? RunnerV2.RunAdapter(fixturesV2, engine, includePathological)
                    : RunnerV2.RunSelfCheck(fixturesV2, includePathological, engine.Capabilities, propose, Console.Out);
            PrintRunReportV2(reportV2, engine);
            anyRan |= reportV2.Passed > 0 || reportV2.Failed > 0;
            anyFailed |= reportV2.Failed > 0;
        }

        if (!anyRan)
        {
            Console.Error.WriteLine("no fixtures actually ran (all excluded or skipped) -- treating this as an error");
            return 2;
        }

        return anyFailed ? 1 : 0;
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
                $"{report.ExcludedPathologicalCount} pathological fixture(s) excluded by default (--include-pathological to run)"
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

    private static void PrintRunReportV2(RunnerV2.RunReportV2 report, IEngine engine)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"[v2] engine: {engine.Name}, capabilities: [{string.Join(",", engine.Capabilities.OrderBy(c => c))}]"
        );
        if (report.ExcludedPathologicalCount > 0)
        {
            Console.WriteLine(
                $"{report.ExcludedPathologicalCount} pathological (budget_ms) v2 fixture(s) excluded by default (--include-pathological to run)"
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
            $"[v2] totals: {report.Passed} passed, {report.Failed} failed, {report.Skipped} skipped (of {report.Results.Count} attempted)"
        );
    }

    // "Tracing" is the one construct the suite deliberately never covers (it was never in
    // expected.tsv's domain) -- see docs/conformance-language-suite-plan.md sections 3 and 7 (G4).
    private const string OutOfScopeConstruct = "Tracing (TraceType)";

    private static void RunCoverageReportV2(List<FixtureV2> fixturesV2, string fixturesRoot, string constructsPath)
    {
        string coverageCsvPath = Path.Combine(fixturesRoot, "coverage.csv");
        string rulesCsvPath = Path.Combine(fixturesRoot, "rules.csv");
        CoverageReportV2.CoverageResultV2 result = CoverageReportV2.WriteCsvs(
            fixturesV2,
            coverageCsvPath,
            rulesCsvPath
        );

        Console.WriteLine();
        Console.WriteLine("v2 coverage report");
        Console.WriteLine("===================");
        Console.WriteLine($"wrote {coverageCsvPath}");
        Console.WriteLine($"wrote {rulesCsvPath}");

        // Absolute construct-coverage check against constructs.txt -- the v2-native replacement for the
        // v1 CoverageReport zero-coverage rollup (which goes inert when the v1 tree is deleted). G4
        // acceptance: every construct except Tracing is covered.
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
            foreach (CoverageReportV2.DeadRule dead in result.DeadRules)
                Console.WriteLine($"  {dead.FixtureId}: rule '{dead.RuleId}'");
        }
        else
        {
            Console.WriteLine("0 dead rules across all v2 grammars.");
        }
    }

    private static int RunCoverageReport(List<Fixture> fixtures, string constructsPath)
    {
        List<string> checklist = CoverageReport.LoadConstructChecklist(constructsPath);
        CoverageReportResult coverage = CoverageReport.Build(fixtures, checklist);

        Console.WriteLine();
        Console.WriteLine("Coverage report");
        Console.WriteLine("===============");
        foreach (ConstructCoverage cc in coverage.Constructs)
        {
            string flags = "";
            if (cc.HasNegative)
                flags += " [negative]";
            if (cc.HasCrossCutting)
                flags += " [cross-cutting]";
            string zero = cc.FixtureCount == 0 ? " *** ZERO COVERAGE ***" : "";
            Console.WriteLine($"{cc.FixtureCount, 3}  {cc.Construct}{flags}{zero}");
        }

        if (coverage.UnknownConstructsInManifests.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("WARNING: construct values used in manifests but not present in constructs.txt:");
            foreach (string c in coverage.UnknownConstructsInManifests)
                Console.WriteLine($"  - {c}");
        }

        int zeroCount = coverage.ZeroCoverage.Count();
        Console.WriteLine();
        Console.WriteLine($"{coverage.Constructs.Count} constructs tracked, {zeroCount} at zero coverage.");

        return 0;
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
              --propose                        v2 self-check only: on a signature mismatch, print
                                                the words.yaml patch that would reconcile it. Never
                                                writes any file.
            """
        );
    }
}
