using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

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
        string coverageOutput = null;
        bool propose = false;
        bool checkManifest = false;

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
                case "--coverage-output":
                    if (!TryGetNextArg(args, ref i, "--coverage-output", out coverageOutput))
                        return 2;
                    break;
                case "--propose":
                    propose = true;
                    break;
                case "--check-manifest":
                    checkManifest = true;
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

        foreach (string warning in SuitePinCheck.Warnings(fixturesRoot))
            Console.Error.WriteLine($"warning: {warning}");

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
            Console.Error.WriteLine(
                $"no fixtures discovered under '{fixturesRoot}'; if this is the conformance/ clone of the suite, "
                    + "run: dotnet build src/SIL.Machine.Morphology.HermitCrab.Conformance"
            );
            return 2;
        }

        if (checkManifest)
            return RunCheckManifest(fixturesRoot);

        if (coverageReport)
        {
            constructsPath ??= Path.Combine(fixturesRoot, "constructs.txt");
            return RunCoverageReport(fixtures, fixturesRoot, constructsPath, coverageOutput);
        }

        RunReport report;
        string engineName;
        IReadOnlySet<string> engineCapabilities;
        if (adapterTemplate != null)
        {
            var engine = new AdapterEngine(adapterTemplate, ParseCapabilities(capabilitiesArg ?? ""));
            report = Runner.RunAdapter(fixtures, engine, includePathological);
            engineName = engine.Name;
            engineCapabilities = engine.Capabilities;
        }
        else
        {
            engineName = "self-check";
            engineCapabilities = capabilitiesProvided ? ParseCapabilities(capabilitiesArg) : Runner.DefaultCapabilities;
            report = Runner.RunSelfCheck(fixtures, includePathological, engineCapabilities, propose, Console.Out);
        }
        PrintRunReport(report, engineName, engineCapabilities);

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

    private static void PrintRunReport(RunReport report, string engineName, IReadOnlySet<string> capabilities)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"engine: {engineName}, capabilities: [{string.Join(",", capabilities.OrderBy(c => c, StringComparer.Ordinal))}]"
        );
        if (report.ExcludedPathologicalCount > 0)
        {
            Console.WriteLine(
                $"{report.ExcludedPathologicalCount} pathological run(s) (budget_ms) excluded by default (--include-pathological to run)"
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

        FixtureResult[] upstreamSkips = report.Results.Where(r => r.UpstreamPending).ToArray();
        if (upstreamSkips.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"{upstreamSkips.Length} run(s) skipped for unsatisfied upstream changes:");
            foreach (FixtureResult result in upstreamSkips)
                Console.WriteLine($"  {result.FixtureId}: {result.Reason}");
        }

        Console.WriteLine();
        Console.WriteLine(
            $"totals: {report.Passed} runs passed, {report.Failed} failed, {report.Skipped} skipped "
                + $"(of {report.Results.Count} runs attempted across {report.FixtureCount} fixtures)"
        );
    }

    /// <summary>Generates the fixture manifest in memory after validating every grammar against the DTD and every
    /// words.yaml against its schema, and checks the manifest's invariants. Nothing is committed or written.</summary>
    private static int RunCheckManifest(string suiteRoot)
    {
        string dtd = Path.Combine(suiteRoot, ConformanceManifestGenerator.DtdRelativePath);
        string wordsSchema = Path.Combine(
            suiteRoot,
            WordsSchemaValidation.SchemaRelativePath.Replace('/', Path.DirectorySeparatorChar)
        );

        ConformanceManifest corpus;
        try
        {
            corpus = ConformanceManifestGenerator.Generate(suiteRoot);
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or InvalidDataException or WordsYamlException)
        {
            Console.Error.WriteLine($"cannot compute the manifest under '{suiteRoot}': {ex.Message}");
            return 2;
        }

        // Validate every fixture before generating, so a rejected one is named.
        foreach (ManifestFixture fixture in corpus.Fixtures)
        {
            IReadOnlyList<string> messages;
            try
            {
                messages = GrammarValidation.Validate(
                    Path.Combine(suiteRoot, fixture.GrammarPath.Replace('/', Path.DirectorySeparatorChar)),
                    dtd
                );
            }
            catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"{fixture.FixtureId}: grammar cannot be read: {ex.Message}");
                return 2;
            }
            if (messages.Count != 0)
            {
                Console.Error.WriteLine($"{fixture.FixtureId}: grammar does not validate against {corpus.DtdPath}");
                foreach (string message in messages)
                    Console.Error.WriteLine($"  {message}");
                return 2;
            }

            messages = WordsSchemaValidation.Validate(
                Path.Combine(suiteRoot, fixture.WordsPath.Replace('/', Path.DirectorySeparatorChar)),
                wordsSchema
            );
            if (messages.Count == 0)
                continue;
            Console.Error.WriteLine(
                $"{fixture.FixtureId}: words.yaml does not conform to {WordsSchemaValidation.SchemaRelativePath}"
            );
            foreach (string message in messages)
                Console.Error.WriteLine($"  {message}");
            return 2;
        }

        // The manifest is derived, not committed: this run generates it and checks its invariants.
        string json = ManifestJson.Serialize(corpus);
        if (!string.Equals(json, ManifestJson.Serialize(ConformanceManifestGenerator.Generate(suiteRoot)), StringComparison.Ordinal))
        {
            Console.Error.WriteLine("the manifest is not deterministic: two generations of the same suite differ");
            return 1;
        }
        string[] ids = corpus.Fixtures.Select(f => f.FixtureId).ToArray();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length || !ids.SequenceEqual(ids.OrderBy(i => i, StringComparer.Ordinal)))
        {
            Console.Error.WriteLine("the manifest's fixture ids must be unique and in ordinal order");
            return 1;
        }
        foreach (ManifestFixture fixture in corpus.Fixtures)
        {
            foreach (string relativePath in new[] { fixture.GrammarPath, fixture.WordsPath })
            {
                if (!File.Exists(Path.Combine(suiteRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))))
                {
                    Console.Error.WriteLine($"{fixture.FixtureId}: the manifest names '{relativePath}', which does not exist");
                    return 1;
                }
            }
        }

        Console.WriteLine($"manifest generated and valid ({corpus.Fixtures.Count} fixtures)");
        return 0;
    }

    private const string OutOfScopeConstruct = "Tracing (TraceType)";

    /// <summary>Recomputes coverage.csv and rules.csv without touching the fixtures root, then returns 1 if an in-scope construct has no coverage, a blocked_by label is not a
    /// grammar rule id, a rule no word exercises is missing from <c>dead-rules-allowed.tsv</c>, or an allowed rule
    /// is no longer dead; 0 otherwise. The suite owns both CSVs, so they are written only into
    /// <paramref name="coverageOutput"/>, never into the fixtures root.</summary>
    private static int RunCoverageReport(
        List<Fixture> fixtures,
        string fixturesRoot,
        string constructsPath,
        string coverageOutput
    )
    {
        bool failed = false;
        string scratch = Path.Combine(Path.GetTempPath(), $"hc-coverage-report-{Guid.NewGuid():N}");
        Directory.CreateDirectory(scratch);
        CoverageReport.CoverageResult result;
        try
        {
            string freshCoverage = Path.Combine(scratch, "coverage.csv");
            string freshRules = Path.Combine(scratch, "rules.csv");
            result = CoverageReport.WriteCsvs(fixtures, freshCoverage, freshRules);

            Console.WriteLine();
            Console.WriteLine("coverage report");
            Console.WriteLine("===============");
            Console.WriteLine("coverage.csv and rules.csv are generated for this run; the suite does not commit them");

            if (coverageOutput != null)
            {
                string fixturesFull = Path.TrimEndingDirectorySeparator(Path.GetFullPath(fixturesRoot));
                string outputFull = Path.TrimEndingDirectorySeparator(Path.GetFullPath(coverageOutput));
                if (
                    outputFull.Equals(fixturesFull, StringComparison.Ordinal)
                    || outputFull.StartsWith(fixturesFull + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                )
                {
                    Console.Error.WriteLine(
                        $"--coverage-output {coverageOutput} is inside the fixtures root; write into a separate clone of the suite, not the suite clone"
                    );
                    return 2;
                }

                Directory.CreateDirectory(outputFull);
                foreach (string name in new[] { "coverage.csv", "rules.csv" })
                {
                    File.Copy(Path.Combine(scratch, name), Path.Combine(outputFull, name), overwrite: true);
                    Console.WriteLine($"wrote {Path.Combine(outputFull, name)}");
                }
            }
        }
        finally
        {
            Directory.Delete(scratch, recursive: true);
        }

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
                failed = true;
                Console.WriteLine($"*** {uncovered.Count} CONSTRUCT(S) AT ZERO COVERAGE ***");
                foreach (string c in uncovered)
                    Console.WriteLine($"  {c}");
            }
        }
        else
        {
            failed = true;
            Console.WriteLine();
            Console.WriteLine($"*** CONSTRUCT CHECKLIST NOT FOUND: {constructsPath} ***");
        }

        if (result.UnknownBlockedBy.Count > 0)
        {
            failed = true;
            Console.WriteLine();
            Console.WriteLine(
                $"*** {result.UnknownBlockedBy.Count} BLOCKED_BY LABEL(S) ARE NOT RULE IDS IN THEIR FIXTURE'S GRAMMAR ***"
            );
            foreach (CoverageReport.UnknownBlockedBy unknown in result.UnknownBlockedBy)
                Console.WriteLine($"  {unknown.FixtureId}: word '{unknown.Word}' blocked_by '{unknown.Label}'");
        }

        string allowlistPath = Path.Combine(fixturesRoot, "dead-rules-allowed.tsv");
        Dictionary<(string FixtureId, string RuleId), string> allowed = File.Exists(allowlistPath)
            ? CoverageReport.LoadDeadRuleAllowlist(allowlistPath)
            : new Dictionary<(string FixtureId, string RuleId), string>();
        (List<CoverageReport.DeadRule> unexpected, List<(string FixtureId, string RuleId)> stale) =
            CoverageReport.CheckDeadRules(result.DeadRules, allowed);

        Console.WriteLine();
        Console.WriteLine(
            $"dead rules: {result.DeadRules.Count} (exercised by zero words), "
                + $"{result.DeadRules.Count - unexpected.Count} allowed by {Path.GetFileName(allowlistPath)}"
        );
        if (unexpected.Count > 0)
        {
            failed = true;
            Console.WriteLine($"*** {unexpected.Count} DEAD RULE(S) NOT IN {Path.GetFileName(allowlistPath)} ***");
            foreach (CoverageReport.DeadRule dead in unexpected)
                Console.WriteLine($"  {dead.FixtureId}: rule '{dead.RuleId}'");
        }
        if (stale.Count > 0)
        {
            failed = true;
            Console.WriteLine($"*** {stale.Count} ALLOWED RULE(S) NOW EXERCISED; REMOVE THEIR ENTRIES ***");
            foreach ((string fixtureId, string ruleId) in stale)
                Console.WriteLine($"  {fixtureId}: rule '{ruleId}'");
        }
        return failed ? 1 : 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            usage: hc-conformance --fixtures <path> [options]

            Runs every fixture under <path> (the conformance/ clone, or conformance/stress) and compares
            each word with its words.yaml: against the C# engine in process, or through --adapter.

            options:
              --adapter "<command template>"   Run fixtures through an external engine adapter.
                                                Template placeholders: {grammar} {words} {output}
                                                and {morpher}. Omit to run the harness's self-check mode
                                                (in-process C# engine) instead.
              --capabilities <comma-list>      Capability set the engine under test declares (e.g.
                                                "phonology", or "" for none). Defaults to the full set
                                                in self-check mode, empty in --adapter mode. A fixture that
                                                requires more is skipped.
              --include-pathological           Also run fixtures with a budget_ms (excluded by default).
              --coverage-report                Print a coverage report instead of running fixtures;
                                                exits 1 on an
                                                uncovered construct or an unallowed dead rule. Writes
                                                nothing under --fixtures.
              --coverage-output <dir>          With --coverage-report, also write the fresh
                                                coverage.csv and rules.csv into <dir>, which must be
                                                outside --fixtures (a clone of the suite).
              --constructs <path>              Construct checklist file for --coverage-report
                                                (default: <fixtures>/constructs.txt).
              --check-manifest                 Generate the manifest in memory and check its invariants, and every
                                                grammar and words.yaml against the suite's DTD and schema. Writes
                                                nothing.
              --propose                        Self-check only: on a signature mismatch, print the words.yaml
                                                patch that would reconcile it. Never writes any file.
            """
        );
    }
}
