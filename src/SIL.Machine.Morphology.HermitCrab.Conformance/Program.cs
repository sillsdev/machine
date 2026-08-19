using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        bool semanticCoverage = false;
        bool writeCoverageBaseline = false;
        bool proposeSemanticCatalog = false;
        bool generateManifest = false;
        bool checkManifest = false;
        var proposedAuditedScopes = new List<string>();
        bool counterfactual = false;
        string mutantGrammar = null;
        string mutantWords = null;
        bool writeCounterfactual = false;
        bool coverageEvidence = false;
        bool writeCoverageEvidence = false;
        bool ruleInteractionPairs = false;
        bool writeRuleInteractionPairs = false;
        bool interfaceInventory = false;
        bool writeInterfaceInventory = false;
        bool interactionChains = false;
        bool writeInteractionChains = false;
        bool dataflowObligations = false;
        bool writeDataflowObligations = false;
        bool coverageTraceability = false;
        bool writeCoverageTraceability = false;
        bool evidenceCards = false;
        bool writeEvidenceCards = false;
        bool engineGateInventory = false;
        bool writeEngineGateInventory = false;
        bool gateObligations = false;
        bool writeGateObligations = false;
        string repositoryRoot = null;

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
                case "--semantic-coverage":
                    semanticCoverage = true;
                    break;
                case "--write-coverage-baseline":
                    writeCoverageBaseline = true;
                    break;
                case "--evaluate-mutant":
                    if (!TryGetNextArg(args, ref i, "--evaluate-mutant", out mutantGrammar))
                        return 2;
                    if (!TryGetNextArg(args, ref i, "--evaluate-mutant", out mutantWords))
                        return 2;
                    break;
                case "--counterfactual":
                    counterfactual = true;
                    break;
                case "--write-counterfactual":
                    writeCounterfactual = true;
                    break;
                case "--coverage-evidence":
                    coverageEvidence = true;
                    break;
                case "--write-coverage-evidence":
                    writeCoverageEvidence = true;
                    break;
                case "--rule-interaction-pairs":
                    ruleInteractionPairs = true;
                    break;
                case "--write-rule-interaction-pairs":
                    writeRuleInteractionPairs = true;
                    break;
                case "--interface-inventory":
                    interfaceInventory = true;
                    break;
                case "--write-interface-inventory":
                    writeInterfaceInventory = true;
                    break;
                case "--interaction-chains":
                    interactionChains = true;
                    break;
                case "--write-interaction-chains":
                    writeInteractionChains = true;
                    break;
                case "--dataflow-obligations":
                    dataflowObligations = true;
                    break;
                case "--write-dataflow-obligations":
                    writeDataflowObligations = true;
                    break;
                case "--coverage-traceability":
                    coverageTraceability = true;
                    break;
                case "--write-coverage-traceability":
                    writeCoverageTraceability = true;
                    break;
                case "--evidence-cards":
                    evidenceCards = true;
                    break;
                case "--write-evidence-cards":
                    writeEvidenceCards = true;
                    break;
                case "--engine-gate-inventory":
                    engineGateInventory = true;
                    break;
                case "--write-engine-gate-inventory":
                    writeEngineGateInventory = true;
                    break;
                case "--gate-obligations":
                    gateObligations = true;
                    break;
                case "--write-gate-obligations":
                    writeGateObligations = true;
                    break;
                case "--propose-semantic-catalog":
                    proposeSemanticCatalog = true;
                    break;
                case "--generate-manifest":
                    generateManifest = true;
                    break;
                case "--check-manifest":
                    checkManifest = true;
                    break;
                case "--audited-source-scope":
                    if (!TryGetNextArg(args, ref i, "--audited-source-scope", out string proposedScope))
                        return 2;
                    proposedAuditedScopes.Add(proposedScope);
                    break;
                case "--repository-root":
                    if (!TryGetNextArg(args, ref i, "--repository-root", out repositoryRoot))
                        return 2;
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

        // One mutant evaluation, in its own process so a non-terminating parse can be killed rather
        // than abandoned; an abandoned in-process task keeps allocating for the life of the sweep.
        if (mutantGrammar != null)
        {
            // Per-word timing goes to stderr, prefixed and distinct from the outcome lines on stdout,
            // so a parent process can recover it without it ever contaminating a baseline diff.
            IReadOnlyList<string> outcomes = SemanticCoverage.CounterfactualGate.EvaluateOneGrammar(
                mutantGrammar,
                mutantWords,
                onTimed: (word, elapsedMs) => Console.Error.WriteLine($"TIME\t{word}\t{elapsedMs}")
            );
            foreach (string outcome in outcomes)
                Console.Out.WriteLine(outcome);

            return 0;
        }

        // Semantic coverage reads the DTD and every grammar.xml directly, so it needs a repository
        // root rather than a discovered fixture set.
        if (
            semanticCoverage
            || writeCoverageBaseline
            || proposeSemanticCatalog
            || generateManifest
            || checkManifest
            || counterfactual
            || writeCounterfactual
            || coverageEvidence
            || writeCoverageEvidence
            || ruleInteractionPairs
            || writeRuleInteractionPairs
            || interfaceInventory
            || writeInterfaceInventory
            || interactionChains
            || writeInteractionChains
            || dataflowObligations
            || writeDataflowObligations
            || coverageTraceability
            || writeCoverageTraceability
            || evidenceCards
            || writeEvidenceCards
            || engineGateInventory
            || writeEngineGateInventory
            || gateObligations
            || writeGateObligations
        )
        {
            repositoryRoot ??= FindRepositoryRoot(Directory.GetCurrentDirectory());
            if (repositoryRoot == null)
            {
                Console.Error.WriteLine("could not locate the repository root; pass --repository-root <path>");
                return 2;
            }

            try
            {
                if (generateManifest || checkManifest)
                    return RunConformanceManifest(repositoryRoot, generateManifest);

                if (coverageEvidence || writeCoverageEvidence)
                    return RunCoverageEvidence(repositoryRoot, writeCoverageEvidence);

                if (ruleInteractionPairs || writeRuleInteractionPairs)
                    return RunRuleInteractionPairs(repositoryRoot, writeRuleInteractionPairs);

                if (interfaceInventory || writeInterfaceInventory)
                    return RunInterfaceInventory(repositoryRoot, writeInterfaceInventory);

                if (interactionChains || writeInteractionChains)
                    return RunInteractionChains(repositoryRoot, writeInteractionChains);
                if (dataflowObligations || writeDataflowObligations)
                    return RunDataflowObligations(repositoryRoot, writeDataflowObligations);
                if (coverageTraceability || writeCoverageTraceability)
                    return RunCoverageTraceability(repositoryRoot, writeCoverageTraceability);
                if (evidenceCards || writeEvidenceCards)
                    return RunEvidenceCards(repositoryRoot, writeEvidenceCards);
                if (engineGateInventory || writeEngineGateInventory)
                    return RunEngineGateInventory(repositoryRoot, writeEngineGateInventory);
                if (gateObligations || writeGateObligations)
                    return RunGateObligations(repositoryRoot, writeGateObligations);

                return counterfactual || writeCounterfactual
                    ? RunCounterfactual(repositoryRoot, writeCounterfactual)
                    : RunSemanticCoverage(repositoryRoot, writeCoverageBaseline, proposeSemanticCatalog, proposedAuditedScopes);
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException or
                    System.Xml.XmlException or WordsYamlException)
            {
                Console.Error.WriteLine($"semantic coverage authority unavailable: {ex.Message}");
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
            RunCoverageReport(fixtures, fixturesRoot, constructsPath);
            return 0;
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

    private static string FindRepositoryRoot(string start)
    {
        for (string dir = start; dir != null; dir = Directory.GetParent(dir)?.FullName)
        {
            if (File.Exists(Path.Combine(dir, "conformance", "constructs.txt")))
                return dir;
        }

        return null;
    }

    private static int RunCounterfactual(string repositoryRoot, bool writeLedger)
    {
        SemanticCoverage.SemanticInventory inventory = SemanticCoverage.GrammarCoverageGate.ReadInventory(
            repositoryRoot
        );
        var timings = new List<SemanticCoverage.WordTiming>();
        var stopwatch = Stopwatch.StartNew();
        // A stalled surface is otherwise invisible until the whole sweep finishes, which is exactly
        // the failure mode this gate exists to bound.
        IReadOnlyList<SemanticCoverage.CounterfactualResult> fresh = SemanticCoverage.CounterfactualLedger.Sweep(
            repositoryRoot,
            inventory,
            (fixtureId, surfaceCount) => Console.Error.WriteLine($"  {fixtureId} ({surfaceCount} surface(s))"),
            timings.Add
        );
        stopwatch.Stop();

        foreach (
            SemanticCoverage.CounterfactualVerdict verdict in Enum.GetValues<SemanticCoverage.CounterfactualVerdict>()
        )
        {
            Console.WriteLine($"  {verdict, -15} {fresh.Count(r => r.Verdict == verdict)}");
        }

        PrintTimingReport(timings, stopwatch.Elapsed);

        // Read BEFORE writing: the verdict below reports whether the ledger checked in at the start
        // of this run matches, and writing must never flip that to green within the same invocation
        // -- otherwise regenerating silently absorbs a real regression, exactly as the coverage
        // baseline gate's own header warns against.
        IReadOnlyList<SemanticCoverage.CounterfactualResult> checkedIn = SemanticCoverage.CounterfactualLedger.Read(
            repositoryRoot
        );
        var checkedInById = checkedIn.ToDictionary(entry => entry.SurfaceId, StringComparer.Ordinal);
        var freshById = fresh.ToDictionary(entry => entry.SurfaceId, StringComparer.Ordinal);

        string[] added = freshById
            .Keys.Except(checkedInById.Keys, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] removed = checkedInById
            .Keys.Except(freshById.Keys, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] changed = freshById
            .Keys.Intersect(checkedInById.Keys, StringComparer.Ordinal)
            .Where(id => freshById[id] != checkedInById[id])
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        foreach (string id in added)
            Console.Error.WriteLine($"NEW SURFACE   {id}: {fresh.First(r => r.SurfaceId == id).Verdict}");
        foreach (string id in removed)
        {
            Console.Error.WriteLine(
                $"GONE SURFACE  {id} (delete from {SemanticCoverage.CounterfactualLedger.RelativePath})"
            );
        }
        foreach (string id in changed)
            Console.Error.WriteLine($"CHANGED       {id}: {checkedInById[id].Verdict} -> {freshById[id].Verdict}");

        if (writeLedger)
        {
            SemanticCoverage.CounterfactualLedger.Write(repositoryRoot, fresh);
            Console.WriteLine($"wrote {SemanticCoverage.CounterfactualLedger.RelativePath} ({fresh.Count} surfaces)");
        }

        // A surface must end as authoritative evidence or as an actually verified proof. The checked-in
        // proof file is input to CoverageCompletenessGate, never a bypass around it.
        IReadOnlyList<SemanticCoverage.ImpossibilityProof> proofs = SemanticCoverage.ImpossibilityProofs.Read(
            repositoryRoot
        );
        IReadOnlyList<SemanticCoverage.CoverageItem> items = fresh
            .Select(result => new SemanticCoverage.CoverageItem(
                result.SurfaceId,
                SemanticCoverage.CoverageItemKind.Surface,
                result.SurfaceId.StartsWith(SemanticCoverage.GrammarFeatureUsage.EnumPrefix, StringComparison.Ordinal)
                    ? "dtd-enum"
                    : "dtd-element",
                result.FixtureId
            ))
            .OrderBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        IReadOnlyList<SemanticCoverage.Evidence> evidence = SemanticCoverage.CoverageEvidencePipeline.BuildEvidence(fresh);
        IReadOnlyList<SemanticCoverage.Proof> coverageProofs = proofs
            .Select(proof => new SemanticCoverage.Proof(proof.SurfaceId, proof.Kind, proof.Evidence))
            .ToArray();
        SemanticCoverage.CompletenessReport completeness = SemanticCoverage.CoverageCompletenessGate.Evaluate(
            items,
            evidence,
            coverageProofs
        );
        IReadOnlyList<string> staleProofs = SemanticCoverage.ImpossibilityProofs.Stale(fresh, proofs);
        foreach (SemanticCoverage.CoverageResolutionResult item in completeness.Items.Where(
            result => result.Resolution == SemanticCoverage.CoverageResolution.Unresolved
        ))
        {
            Console.Error.WriteLine($"UNACCOUNTED   {item.ItemId}: {item.Detail}");
        }
        foreach (SemanticCoverage.CoverageResolutionResult item in completeness.Items.Where(
            result => result.Resolution == SemanticCoverage.CoverageResolution.Rejected
        ))
        {
            Console.Error.WriteLine($"REJECTED PROOF {item.ItemId}: {item.Detail}");
        }
        foreach (SemanticCoverage.CoverageResolutionResult item in completeness.Items.Where(
            result => result.Resolution == SemanticCoverage.CoverageResolution.Conflicting
        ))
        {
            Console.Error.WriteLine($"CONFLICT      {item.ItemId}: {item.Detail}");
        }
        foreach (string id in completeness.OrphanedProofItemIds)
        {
            Console.Error.WriteLine($"ORPHAN PROOF  {id} (delete from {SemanticCoverage.ImpossibilityProofs.RelativePath})");
        }
        foreach (string id in completeness.OrphanedEvidenceItemIds)
        {
            Console.Error.WriteLine($"ORPHAN EVIDENCE {id} (delete from {SemanticCoverage.EvidenceLedger.RelativePath})");
        }

        foreach (string id in staleProofs)
        {
            Console.Error.WriteLine($"STALE PROOF   {id} (delete from {SemanticCoverage.ImpossibilityProofs.RelativePath})");
        }

        Console.WriteLine(
            $"  evidence: {completeness.Items.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Evidenced)}"
                + $"  proven impossible: {completeness.Items.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Proven)}"
                + $"  rejected proofs: {completeness.Items.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Rejected)}"
                + $"  conflicts: {completeness.Items.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Conflicting)}"
                + $"  orphan evidence: {completeness.OrphanedEvidenceItemIds.Count}"
                + $"  orphan proofs: {completeness.OrphanedProofItemIds.Count}"
                + $"  unaccounted: {completeness.Items.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Unresolved)}"
        );

        if (
            added.Length != 0
            || removed.Length != 0
            || changed.Length != 0
            || !completeness.IsComplete
            || staleProofs.Count != 0
        )
        {
            Console.Error.WriteLine(
                $"counterfactual coverage findings: {added.Length} new, {removed.Length} gone, {changed.Length} changed, "
                    + $"{completeness.Items.Count(r => r.Resolution is SemanticCoverage.CoverageResolution.Unresolved or SemanticCoverage.CoverageResolution.Rejected)} unresolved/rejected, {staleProofs.Count} stale proof(s)"
            );
            return 1;
        }

        Console.WriteLine("every surface is evidenced or proven impossible");
        return 0;
    }

    /// <summary>Generates the fixture manifest, or verifies the checked-in one byte for byte.</summary>
    private static int RunConformanceManifest(string repositoryRoot, bool write)
    {
        string relative = "conformance/generated/hc-conformance-manifest.v1.json";
        string path = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));

        SemanticCoverage.ConformanceManifest corpus = SemanticCoverage.ConformanceManifestGenerator.Generate(repositoryRoot);
        string dtd = Path.Combine(
            repositoryRoot,
            SemanticCoverage.GrammarCoverageGate.DtdRelativePath.Replace('/', Path.DirectorySeparatorChar));
        string wordsSchema = Path.Combine(
            repositoryRoot,
            SemanticCoverage.WordsSchemaValidation.SchemaRelativePath.Replace('/', Path.DirectorySeparatorChar));
        // Validate every fixture before anything is written, so a rejected one cannot leave a
        // half-updated product behind.
        foreach (SemanticCoverage.ManifestFixture fixture in corpus.Fixtures)
        {
            IReadOnlyList<string> messages = SemanticCoverage.GrammarValidation.Validate(
                Path.Combine(repositoryRoot, fixture.GrammarPath.Replace('/', Path.DirectorySeparatorChar)),
                dtd);
            if (messages.Count != 0)
            {
                Console.Error.WriteLine($"{fixture.FixtureId}: grammar does not validate against {corpus.DtdPath}");
                foreach (string message in messages)
                    Console.Error.WriteLine($"  {message}");
                return 2;
            }

            messages = SemanticCoverage.WordsSchemaValidation.Validate(
                Path.Combine(repositoryRoot, fixture.WordsPath.Replace('/', Path.DirectorySeparatorChar)),
                wordsSchema);
            if (messages.Count == 0)
                continue;
            Console.Error.WriteLine(
                $"{fixture.FixtureId}: words.yaml does not conform to {SemanticCoverage.WordsSchemaValidation.SchemaRelativePath}");
            foreach (string message in messages)
                Console.Error.WriteLine($"  {message}");
            return 2;
        }

        string json = SemanticCoverage.ManifestJson.Serialize(corpus) + "\n";
        if (write)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
            Console.WriteLine($"wrote {relative} ({corpus.Fixtures.Count} fixtures, sourceHash {corpus.SourceHash})");
            return 0;
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"{relative} is missing; regenerate with --generate-manifest");
            return 1;
        }

        if (!string.Equals(File.ReadAllText(path).ReplaceLineEndings("\n"), json, StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"{relative} is stale; regenerate with --generate-manifest");
            return 1;
        }

        Console.WriteLine($"{relative} is current ({corpus.Fixtures.Count} fixtures)");
        return 0;
    }

    /// <summary>Recomputes every fixture's rule-interaction denominator and checks it against the checked-in
    /// ledger, or rewrites it.</summary>
    private static int RunRuleInteractionPairs(string repositoryRoot, bool writeLedger)
    {
        var rows = new List<SemanticCoverage.RuleInteractionLedger.Row>();
        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            System.Xml.Linq.XDocument grammar = System.Xml.Linq.XDocument.Load(fixture.GrammarPath);
            rows.AddRange(SemanticCoverage.RuleInteractionLedger.Compute(grammar, fixture.Id));
        }

        var byKind = rows.ToLookup(r => r.PairKind);
        var byRelation = rows.ToLookup(r => r.Relation);
        Console.WriteLine($"rule-interaction pairs: {rows.Count}");
        foreach (SemanticCoverage.StratumPairKind kind in Enum.GetValues<SemanticCoverage.StratumPairKind>())
            Console.WriteLine($"  {kind, -12} {byKind[kind].Count()}");
        foreach (SemanticCoverage.DomainRelation relation in Enum.GetValues<SemanticCoverage.DomainRelation>())
            Console.WriteLine($"  {relation, -12} {byRelation[relation].Count()}");

        string relative = SemanticCoverage.RuleInteractionLedger.RelativePath;
        string path = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        string fresh = SemanticCoverage.RuleInteractionLedger.ToText(rows);

        if (writeLedger)
        {
            SemanticCoverage.RuleInteractionLedger.Write(repositoryRoot, rows);
            Console.WriteLine($"wrote {relative} ({rows.Count} row(s))");
            return 0;
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"{relative} is missing; regenerate with --write-rule-interaction-pairs");
            return 1;
        }

        if (!string.Equals(File.ReadAllText(path).ReplaceLineEndings("\n"), fresh.ReplaceLineEndings("\n"), StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"{relative} is stale; regenerate with --write-rule-interaction-pairs");
            return 1;
        }

        Console.WriteLine($"{relative} is current ({rows.Count} row(s))");
        return 0;
    }

    /// <summary>Recomputes the DTD-derived interface inventory against the real corpus and checks it against
    /// the checked-in ledger, or rewrites it.</summary>
    private static int RunInterfaceInventory(string repositoryRoot, bool writeLedger)
    {
        IReadOnlyList<SemanticCoverage.InterfaceInventoryLedger.Row> rows = SemanticCoverage.InterfaceInventoryLedger.Compute(
            repositoryRoot
        );
        IReadOnlyList<SemanticCoverage.InterfaceJunction> junctions = SemanticCoverage.InterfaceInventoryLedger.ComputeJunctions(
            rows
        );
        int presentCount = rows.Count(r => r.Present);
        int typedEdgeCount = rows.Sum(r => r.ObservedTargetTypes.Count);

        Console.WriteLine($"declared interfaces: {rows.Count}");
        Console.WriteLine($"  present     {presentCount} (structural only -- see interface-witness.tsv for witness)");
        Console.WriteLine($"  not present {rows.Count - presentCount}");
        Console.WriteLine($"typed edges: {typedEdgeCount}");
        Console.WriteLine($"junctions: {junctions.Count}");
        foreach (SemanticCoverage.InterfaceJunction junction in junctions)
            Console.WriteLine($"  {junction.TargetType} ({junction.WriterCount} writer(s), {junction.ReaderCount} reader(s))");

        string relative = SemanticCoverage.InterfaceInventoryLedger.RelativePath;
        string path = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        string fresh = SemanticCoverage.InterfaceInventoryLedger.ToText(rows);

        if (writeLedger)
        {
            SemanticCoverage.InterfaceInventoryLedger.Write(repositoryRoot, rows);
            Console.WriteLine($"wrote {relative} ({rows.Count} row(s))");
            return 0;
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"{relative} is missing; regenerate with --write-interface-inventory");
            return 1;
        }

        if (!string.Equals(File.ReadAllText(path).ReplaceLineEndings("\n"), fresh.ReplaceLineEndings("\n"), StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"{relative} is stale; regenerate with --write-interface-inventory");
            return 1;
        }

        Console.WriteLine($"{relative} is current ({rows.Count} row(s))");
        return 0;
    }

    /// <summary>Recomputes the write/read interaction-chain denominator and checks it against the
    /// checked-in ledger, or rewrites it.</summary>
    private static int RunInteractionChains(string repositoryRoot, bool writeLedger)
    {
        IReadOnlyList<SemanticCoverage.InteractionChainLedger.Row> rows = SemanticCoverage.InteractionChainLedger.Compute(
            repositoryRoot
        );
        IReadOnlyList<SemanticCoverage.ChainJunction> junctions = SemanticCoverage.InteractionChainLedger.ComputeJunctions(
            repositoryRoot
        );
        int exercisedCount = rows.Count(r => r.Exercised);
        int hazardousCount = rows.Count(r => r.Hazardous);

        Console.WriteLine($"junctions: {junctions.Count}");
        foreach (SemanticCoverage.ChainJunction junction in junctions)
            Console.WriteLine($"  {junction.PayloadType} ({junction.Writers.Count} writer(s), {junction.Readers.Count} reader(s))");
        Console.WriteLine($"interaction chains: {rows.Count}");
        Console.WriteLine($"  exercised   {exercisedCount}");
        Console.WriteLine($"  unexercised {rows.Count - exercisedCount}");
        Console.WriteLine($"  hazardous   {hazardousCount}");

        string relative = SemanticCoverage.InteractionChainLedger.RelativePath;
        string path = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        string fresh = SemanticCoverage.InteractionChainLedger.ToText(rows);

        if (writeLedger)
        {
            SemanticCoverage.InteractionChainLedger.Write(repositoryRoot, rows);
            Console.WriteLine($"wrote {relative} ({rows.Count} row(s))");
            return 0;
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"{relative} is missing; regenerate with --write-interaction-chains");
            return 1;
        }

        if (!string.Equals(File.ReadAllText(path).ReplaceLineEndings("\n"), fresh.ReplaceLineEndings("\n"), StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"{relative} is stale; regenerate with --write-interaction-chains");
            return 1;
        }

        Console.WriteLine($"{relative} is current ({rows.Count} row(s))");
        return 0;
    }

    /// <summary>Recomputes the data-flow/MC/DC obligation-matrix cells and checks them against the
    /// checked-in ledger, or rewrites it.</summary>
    private static int RunDataflowObligations(string repositoryRoot, bool writeLedger)
    {
        IReadOnlyList<SemanticCoverage.DataflowObligationLedger.Row> rows = SemanticCoverage.DataflowObligationLedger.Compute(
            repositoryRoot
        );
        int satisfied = rows.Count(r => r.Status == SemanticCoverage.ObligationStatus.Satisfied);
        int notSatisfied = rows.Count(r => r.Status == SemanticCoverage.ObligationStatus.NotSatisfied);
        int unknown = rows.Count(r => r.Status == SemanticCoverage.ObligationStatus.Unknown);

        Console.WriteLine($"obligation cells: {rows.Count}");
        Console.WriteLine($"  satisfied     {satisfied}");
        Console.WriteLine($"  not_satisfied {notSatisfied}");
        Console.WriteLine($"  unknown       {unknown}");

        string relative = SemanticCoverage.DataflowObligationLedger.RelativePath;
        string path = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        string fresh = SemanticCoverage.DataflowObligationLedger.ToText(rows);

        if (writeLedger)
        {
            SemanticCoverage.DataflowObligationLedger.Write(repositoryRoot, rows);
            Console.WriteLine($"wrote {relative} ({rows.Count} row(s))");
            return 0;
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"{relative} is missing; regenerate with --write-dataflow-obligations");
            return 1;
        }

        if (!string.Equals(File.ReadAllText(path).ReplaceLineEndings("\n"), fresh.ReplaceLineEndings("\n"), StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"{relative} is stale; regenerate with --write-dataflow-obligations");
            return 1;
        }

        Console.WriteLine($"{relative} is current ({rows.Count} row(s))");
        return 0;
    }

    /// <summary>Recomputes the engine-gate inventory (one row per SIL.Machine.Morphology.HermitCrab.
    /// FailureReason member) -- mechanically scanned raise sites, hand-verified DTD attributes, and an
    /// actual traced engine sweep for witness evidence -- and checks it against the checked-in ledger,
    /// or rewrites it.</summary>
    private static int RunEngineGateInventory(string repositoryRoot, bool writeLedger)
    {
        Console.WriteLine("tracing every non-pathological, non-crash fixture's words for engine-gate evidence...");
        IReadOnlyList<SemanticCoverage.EngineGateInventoryLedger.Row> rows = SemanticCoverage.EngineGateInventoryLedger.Compute(
            repositoryRoot
        );
        int witnessed = rows.Count(r => r.Status == SemanticCoverage.EngineGateStatus.Witnessed);
        int unreached = rows.Count(r => r.Status == SemanticCoverage.EngineGateStatus.Unreached);
        int noDtdAttribute = rows.Count(r => r.DtdAttributes == "-");

        Console.WriteLine($"engine gates: {rows.Count}");
        Console.WriteLine($"  witnessed        {witnessed}");
        Console.WriteLine($"  unreached        {unreached}");
        Console.WriteLine($"  no DTD attribute {noDtdAttribute}");
        foreach (SemanticCoverage.EngineGateInventoryLedger.Row row in rows.Where(r => r.Status == SemanticCoverage.EngineGateStatus.Unreached))
            Console.Error.WriteLine($"UNREACHED  {row.Gate}  (raise sites: {row.RaiseSites})");

        string relative = SemanticCoverage.EngineGateInventoryLedger.RelativePath;
        string path = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        string fresh = SemanticCoverage.EngineGateInventoryLedger.ToText(rows);

        if (writeLedger)
        {
            SemanticCoverage.EngineGateInventoryLedger.Write(repositoryRoot, rows);
            Console.WriteLine($"wrote {relative} ({rows.Count} row(s))");
            return 0;
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"{relative} is missing; regenerate with --write-engine-gate-inventory");
            return 1;
        }

        if (!string.Equals(File.ReadAllText(path).ReplaceLineEndings("\n"), fresh.ReplaceLineEndings("\n"), StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"{relative} is stale; regenerate with --write-engine-gate-inventory");
            return 1;
        }

        Console.WriteLine($"{relative} is current ({rows.Count} row(s))");
        return 0;
    }

    /// <summary>Recomputes the gate-keyed MC/DC obligation ledger (Blocked/Control arm per
    /// FailureReason gate, conformance/gate-obligations.tsv) -- reads the already-checked-in
    /// engine-gate-inventory.tsv, interface-witness.tsv and fieldworks-producibility.tsv where it can,
    /// falling back to a fresh severance run only where those do not already cover an attribute -- and
    /// checks it against the checked-in ledger, or rewrites it.</summary>
    private static int RunGateObligations(string repositoryRoot, bool writeLedger)
    {
        Console.WriteLine("evaluating gate-keyed obligations (engine-gate-inventory + interface-witness, with a severance fallback)...");
        IReadOnlyList<SemanticCoverage.GateObligationLedger.Row> rows = SemanticCoverage.GateObligationLedger.Compute(
            repositoryRoot
        );

        int gates = rows.Select(r => r.Gate).Distinct().Count();
        int worthCovering = rows.Count(r => r.WorthCovering == "Yes") / 2;
        int evidenced = rows.Count(r => r.Status == SemanticCoverage.GateArmStatus.Evidenced);
        int notEvidenced = rows.Count(r => r.Status == SemanticCoverage.GateArmStatus.NotEvidenced);
        int blockedEvidenced = rows.Count(r => r.Arm == "Blocked" && r.Status == SemanticCoverage.GateArmStatus.Evidenced);
        int controlEvidenced = rows.Count(r => r.Arm == "Control" && r.Status == SemanticCoverage.GateArmStatus.Evidenced);
        int bothEvidenced = rows
            .Where(r => r.Status == SemanticCoverage.GateArmStatus.Evidenced)
            .GroupBy(r => r.Gate)
            .Count(g => g.Count() == 2);

        Console.WriteLine($"gates: {gates}  obligations (gate x arm): {rows.Count}");
        Console.WriteLine($"  worth_covering (xml_reachable=Yes and flex_producible=Yes) {worthCovering}");
        Console.WriteLine($"  arms evidenced      {evidenced}");
        Console.WriteLine($"  arms not evidenced  {notEvidenced}");
        Console.WriteLine($"    Blocked evidenced {blockedEvidenced}");
        Console.WriteLine($"    Control evidenced {controlEvidenced}");
        Console.WriteLine($"  gates with BOTH arms evidenced {bothEvidenced}");

        string relative = SemanticCoverage.GateObligationLedger.RelativePath;
        string path = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        string fresh = SemanticCoverage.GateObligationLedger.ToText(rows);

        if (writeLedger)
        {
            SemanticCoverage.GateObligationLedger.Write(repositoryRoot, rows);
            Console.WriteLine($"wrote {relative} ({rows.Count} row(s))");
            return 0;
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"{relative} is missing; regenerate with --write-gate-obligations");
            return 1;
        }

        if (!string.Equals(File.ReadAllText(path).ReplaceLineEndings("\n"), fresh.ReplaceLineEndings("\n"), StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"{relative} is stale; regenerate with --write-gate-obligations");
            return 1;
        }

        Console.WriteLine($"{relative} is current ({rows.Count} row(s))");
        return 0;
    }

    /// <summary>Renders (or checks) one reviewable Markdown card per conformance/dataflow-obligations.tsv
    /// cell under conformance/evidence-cards/. Never recomputes any ledger -- purely a rendering of
    /// already-checked-in facts for a human or reviewing agent to read; the only failure mode is staleness.</summary>
    private static int RunEvidenceCards(string repositoryRoot, bool writeCards)
    {
        IReadOnlyList<SemanticCoverage.EvidenceCard> cards = SemanticCoverage.EvidenceCardGenerator.Compute(repositoryRoot);
        IReadOnlyList<SemanticCoverage.DataflowObligationLedger.Row> ledgerRows = SemanticCoverage.DataflowObligationLedger.Read(
            repositoryRoot
        );

        Console.WriteLine($"evidence cards: {cards.Count}");
        foreach (SemanticCoverage.ObligationStatus status in Enum.GetValues<SemanticCoverage.ObligationStatus>())
            Console.WriteLine($"  {status, -13} {ledgerRows.Count(r => r.Status == status)}");

        if (writeCards)
        {
            SemanticCoverage.EvidenceCardGenerator.Write(repositoryRoot, cards);
            Console.WriteLine(
                $"wrote {cards.Count} card(s) to {SemanticCoverage.EvidenceCardGenerator.RelativeDirectory}"
            );
            return 0;
        }

        SemanticCoverage.EvidenceCardDiff diff = SemanticCoverage.EvidenceCardGenerator.Check(repositoryRoot, cards);
        foreach (string detail in diff.Details)
            Console.Error.WriteLine(detail);

        if (!diff.IsCurrent)
        {
            Console.Error.WriteLine(
                $"{SemanticCoverage.EvidenceCardGenerator.RelativeDirectory} is stale ({diff.StaleOrMissingCount} "
                    + $"missing/stale, {diff.ExtraFileCount} extra); regenerate with --write-evidence-cards"
            );
            return 1;
        }

        Console.WriteLine($"{SemanticCoverage.EvidenceCardGenerator.RelativeDirectory} is current ({cards.Count} card(s))");
        return 0;
    }

    /// <summary>
    /// Regenerates (or checks) the four traceability artifacts together: interface-witness.tsv (the
    /// only expensive part -- a severance sweep, same cost class as --write-counterfactual), then the
    /// three cheap joins that read it back: grammar-coverage-ledger.tsv, construct-claim-corroboration.tsv,
    /// and fold-in-candidates.tsv. Even the non-writing (check) path re-runs the severance sweep, so this
    /// is a deliberate, occasional command -- never part of ordinary CI -- not a cheap drift check.
    /// </summary>
    private static int RunCoverageTraceability(string repositoryRoot, bool writeLedgers)
    {
        int WriteOrCheck(string relative, string fresh, Action write)
        {
            if (writeLedgers)
            {
                write();
                Console.WriteLine($"wrote {relative}");
                return 0;
            }

            string path = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"{relative} is missing; regenerate with --write-coverage-traceability");
                return 1;
            }

            bool same = string.Equals(
                File.ReadAllText(path).ReplaceLineEndings("\n"),
                fresh.ReplaceLineEndings("\n"),
                StringComparison.Ordinal
            );
            if (!same)
            {
                Console.Error.WriteLine($"{relative} is stale; regenerate with --write-coverage-traceability");
                return 1;
            }

            Console.WriteLine($"{relative} is current");
            return 0;
        }

        Console.WriteLine("sweeping interface severance witnesses (this re-parses every present interface x fixture)...");
        IReadOnlyList<SemanticCoverage.InterfaceWitnessResult> witnessRows = SemanticCoverage.InterfaceWitnessLedger.Sweep(
            repositoryRoot,
            onFixtureStarted: (fixtureId, count) => Console.Error.WriteLine($"  {fixtureId} ({count} interface(s))")
        );
        int failed = WriteOrCheck(
            SemanticCoverage.InterfaceWitnessLedger.RelativePath,
            SemanticCoverage.InterfaceWitnessLedger.ToText(witnessRows),
            () => SemanticCoverage.InterfaceWitnessLedger.Write(repositoryRoot, witnessRows)
        );

        // The three joins below read interface-witness.tsv back off disk (see their own doc comments
        // for why that is deliberate), so in write mode the write above has to have already landed;
        // WriteOrCheck runs its `write` callback before returning, so that is already guaranteed here.
        string constructsPath = Path.Combine(repositoryRoot, "conformance", "constructs.txt");
        IReadOnlyList<SemanticCoverage.ConstructClaimCorroboration.Row> claimRows =
            SemanticCoverage.ConstructClaimCorroboration.Compute(repositoryRoot, constructsPath);
        failed += WriteOrCheck(
            SemanticCoverage.ConstructClaimCorroboration.RelativePath,
            SemanticCoverage.ConstructClaimCorroboration.ToText(claimRows),
            () => SemanticCoverage.ConstructClaimCorroboration.Write(repositoryRoot, claimRows)
        );

        IReadOnlyList<SemanticCoverage.GrammarCoverageLedger.Row> grammarRows =
            SemanticCoverage.GrammarCoverageLedger.Compute(repositoryRoot);
        failed += WriteOrCheck(
            SemanticCoverage.GrammarCoverageLedger.RelativePath,
            SemanticCoverage.GrammarCoverageLedger.ToText(grammarRows),
            () => SemanticCoverage.GrammarCoverageLedger.Write(repositoryRoot, grammarRows)
        );

        IReadOnlyList<SemanticCoverage.FoldInCandidateLedger.Row> foldInRows =
            SemanticCoverage.FoldInCandidateLedger.Compute(repositoryRoot);
        failed += WriteOrCheck(
            SemanticCoverage.FoldInCandidateLedger.RelativePath,
            SemanticCoverage.FoldInCandidateLedger.ToText(foldInRows),
            () => SemanticCoverage.FoldInCandidateLedger.Write(repositoryRoot, foldInRows)
        );

        return failed == 0 ? 0 : 1;
    }

    /// <summary>
    /// Reads the checked-in Surface ledger (already refreshed by a prior <c>--write-counterfactual</c>
    /// run -- never recomputed here, so this mode's cost is the Ordering sweep alone), runs the Ordering
    /// sweep, combines both into the CoverageItem inventory, recomputes completeness, and prints the
    /// breakdown by item kind and by counterexample kind. Never blends Word and LoadFailure counts, and
    /// never blends Surface and Ordering counts.
    /// </summary>
    private static int RunCoverageEvidence(string repositoryRoot, bool writeLedger)
    {
        IReadOnlyList<SemanticCoverage.CounterfactualResult> surfaceResults = SemanticCoverage.CounterfactualLedger.Read(
            repositoryRoot
        );
        if (surfaceResults.Count == 0)
        {
            Console.Error.WriteLine(
                $"{SemanticCoverage.CounterfactualLedger.RelativePath} is empty; run --write-counterfactual first"
            );
            return 2;
        }

        Console.WriteLine("combined Surface+Ordering inventory");
        Console.WriteLine("------------------------------------");
        IReadOnlyList<SemanticCoverage.CounterfactualResult> orderingResults = SemanticCoverage.CounterfactualLedger.SweepOrdering(
            repositoryRoot,
            (fixtureId, itemCount) => Console.Error.WriteLine($"  ordering: {fixtureId} ({itemCount} item(s))")
        );

        IReadOnlyList<SemanticCoverage.CoverageItem> items = SemanticCoverage.CoverageEvidencePipeline.BuildItems(
            surfaceResults,
            orderingResults
        );
        IReadOnlyList<SemanticCoverage.Evidence> evidence = SemanticCoverage.CoverageEvidencePipeline.BuildEvidence(
            surfaceResults.Concat(orderingResults).ToArray()
        );
        var evidencedIds = evidence.Select(e => e.ItemId).ToHashSet(StringComparer.Ordinal);
        SemanticCoverage.CoverageItem[] nonEvidencedOrdering = items
            .Where(item => item.Kind == SemanticCoverage.CoverageItemKind.Ordering && !evidencedIds.Contains(item.Id))
            .ToArray();
        IReadOnlyList<SemanticCoverage.Proof> proofs = SemanticCoverage.CoverageEvidencePipeline.BuildProofs(
            repositoryRoot,
            nonEvidencedOrdering
        );

        SemanticCoverage.CompletenessReport report = SemanticCoverage.CoverageCompletenessGate.Evaluate(
            items,
            evidence,
            proofs,
            SemanticCoverage.CoverageEvidencePipeline.GrammarLoader(repositoryRoot)
        );

        var kindByItemId = items.ToDictionary(item => item.Id, item => item.Kind, StringComparer.Ordinal);
        foreach (SemanticCoverage.CoverageItemKind kind in Enum.GetValues<SemanticCoverage.CoverageItemKind>())
        {
            SemanticCoverage.CoverageResolutionResult[] ofKind = report
                .Items.Where(result => kindByItemId[result.ItemId] == kind)
                .ToArray();
            if (ofKind.Length == 0)
                continue;

            Console.WriteLine($"  {kind} ({ofKind.Length} item(s)):");
            Console.WriteLine(
                $"    evidenced (Word):         {ofKind.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Evidenced && r.CounterexampleKind == SemanticCoverage.CounterexampleKind.Word)}"
            );
            Console.WriteLine(
                $"    evidenced (LoadFailure):  {ofKind.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Evidenced && r.CounterexampleKind == SemanticCoverage.CounterexampleKind.LoadFailure)}"
            );
            Console.WriteLine($"    proven:                   {ofKind.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Proven)}");
            Console.WriteLine($"    unresolved (gap):         {ofKind.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Unresolved)}");
            Console.WriteLine($"    conflicting:              {ofKind.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Conflicting)}");
            Console.WriteLine($"    rejected proof:           {ofKind.Count(r => r.Resolution == SemanticCoverage.CoverageResolution.Rejected)}");
        }

        Console.WriteLine($"  total: {items.Count} item(s), complete: {report.IsComplete}");
        if (report.OrphanedEvidenceItemIds.Count != 0)
            Console.Error.WriteLine($"  orphaned evidence row(s): {string.Join(", ", report.OrphanedEvidenceItemIds)}");
        if (report.OrphanedProofItemIds.Count != 0)
            Console.Error.WriteLine($"  orphaned proof(s): {string.Join(", ", report.OrphanedProofItemIds)}");

        if (writeLedger)
        {
            IReadOnlyList<SemanticCoverage.EvidenceLedger.Row> rows = SemanticCoverage.CoverageEvidencePipeline.BuildLedgerRows(items, evidence);
            SemanticCoverage.EvidenceLedger.Write(repositoryRoot, rows);
            Console.WriteLine($"wrote {SemanticCoverage.EvidenceLedger.RelativePath} ({rows.Count} row(s))");
        }

        // This is authoritative, unlike the additive freshness stats above: every non-evidenced item
        // must either pass a recomputed proof or remain an explicit failure.
        return report.IsComplete ? 0 : 1;
    }

    // 2s is the sweep's real per-word target; the child-process kill is only the safety net behind
    // it, so a word over 2s is worth naming even when the run it belongs to still finished in time.
    private static readonly TimeSpan SlowWordThreshold = TimeSpan.FromSeconds(2);

    private static void PrintTimingReport(IReadOnlyList<SemanticCoverage.WordTiming> timings, TimeSpan elapsed)
    {
        long thresholdMs = (long)SlowWordThreshold.TotalMilliseconds;
        SemanticCoverage.WordTiming[] baseline = timings.Where(t => t.Phase == "baseline").ToArray();
        SemanticCoverage.WordTiming[] mutant = timings.Where(t => t.Phase == "mutant").ToArray();
        SemanticCoverage.WordTiming[] slowBaseline = baseline
            .Where(t => t.ElapsedMs >= thresholdMs)
            .OrderByDescending(t => t.ElapsedMs)
            .ToArray();
        SemanticCoverage.WordTiming[] slowMutant = mutant
            .Where(t => t.ElapsedMs >= thresholdMs)
            .OrderByDescending(t => t.ElapsedMs)
            .ToArray();
        int slowMutantEvaluations = slowMutant.Select(t => (t.FixtureId, t.SurfaceId)).Distinct().Count();

        Console.WriteLine();
        Console.WriteLine(
            $"timing: {baseline.Length} unmutated word parse(s), {mutant.Length} mutant word parse(s), "
                + $"sweep took {elapsed.TotalSeconds:0.0}s"
        );
        Console.WriteLine($"  unmutated word(s) >= {SlowWordThreshold.TotalSeconds:0}s: {slowBaseline.Length}");
        foreach (SemanticCoverage.WordTiming t in slowBaseline)
            Console.Error.WriteLine($"SLOW BASELINE  {t.FixtureId}  '{t.Word}'  {t.ElapsedMs}ms");
        Console.WriteLine(
            $"  mutant evaluation(s) with a word >= {SlowWordThreshold.TotalSeconds:0}s: {slowMutantEvaluations}"
        );
        foreach (SemanticCoverage.WordTiming t in slowMutant.Take(20))
            Console.Error.WriteLine($"SLOW MUTANT    {t.FixtureId}  {t.SurfaceId}  '{t.Word}'  {t.ElapsedMs}ms");
    }

    private static int RunSemanticCoverage(
        string repositoryRoot,
        bool writeBaseline,
        bool proposeCatalog,
        IReadOnlyCollection<string> proposedAuditedScopes)
    {
        // A proposal is review material, not an authority verdict. If the curated catalog already
        // names scopes, use that live source snapshot; an empty catalog remains on the explicit
        // legacy DTD-only path rather than guessing scopes or silently becoming authoritative.
        if (proposeCatalog)
        {
            SemanticCoverage.SemanticInventory proposal;
            IReadOnlyList<string> proposalScopes;
            try
            {
                SemanticCoverage.SemanticCatalog existingCatalog = SemanticCoverage.CatalogBootstrap.Load(repositoryRoot);
                proposalScopes = proposedAuditedScopes.Count != 0
                    ? proposedAuditedScopes.Distinct(StringComparer.Ordinal).OrderBy(scope => scope, StringComparer.Ordinal).ToArray()
                    : existingCatalog.AuditedSourceScopes;
                if (proposalScopes.Count == 0)
                {
                    Console.Error.WriteLine(
                        "semantic catalog proposal requires one or more --audited-source-scope <canonical-id> arguments "
                            + "when the checked-in catalog has no audited scopes");
                    return 2;
                }

                proposal = SemanticCoverage.GraphSemanticCensus.Read(
                    repositoryRoot, proposalScopes, System.Threading.CancellationToken.None);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or ArgumentException or InvalidOperationException)
            {
                Console.Error.WriteLine($"semantic catalog proposal unavailable: {ex.Message}");
                return 2;
            }
            SemanticCoverage.CatalogBootstrap.WriteProposal(Console.Out, proposal, proposalScopes);
            Console.Error.WriteLine("semantic catalog proposal only; canonical catalog was not changed and no authority verdict was produced");
            return 1;
        }

        SemanticCoverage.SemanticCatalog catalog;
        try
        {
            // Load the catalog before touching source roots. Empty scopes are a controlled audit
            // failure; they must never be replaced by guessed roots or a DTD-only fallback.
            catalog = SemanticCoverage.CatalogBootstrap.Load(repositoryRoot);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            Console.Error.WriteLine($"semantic coverage authority unavailable: malformed or missing catalog ({ex.Message})");
            return 2;
        }

        SemanticCoverage.SemanticInventory inventory;
        try
        {
            inventory = SemanticCoverage.GraphSemanticCensus.Read(
                repositoryRoot,
                catalog.AuditedSourceScopes,
                System.Threading.CancellationToken.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or FormatException)
        {
            Console.Error.WriteLine($"semantic coverage authority unavailable: source snapshot could not be read ({ex.Message})");
            return 2;
        }

        SemanticCoverage.GrammarCoverageResult result = SemanticCoverage.GrammarCoverageGate.Compute(
            repositoryRoot,
            inventory
        );

        Console.WriteLine($"generated surfaces:  {inventory.Surfaces.Count}");
        Console.WriteLine($"grammar-observable:  {result.Observable.Count}");
        Console.WriteLine($"covered by fixtures: {result.Covered.Count}");
        Console.WriteLine($"uncovered:           {result.Uncovered.Count}");

        IReadOnlyList<SemanticCoverage.GrammarCoverageGate.LedgerEntry> existing =
            SemanticCoverage.GrammarCoverageGate.ReadBaseline(repositoryRoot);
        IReadOnlyList<SemanticCoverage.GrammarCoverageGate.LedgerEntry> classified =
            SemanticCoverage.GrammarCoverageGate.Classify(repositoryRoot, result.Uncovered, existing);
        int dead = classified.Count(entry => entry.Classification == SemanticCoverage.GrammarCoverageGate.DeadSchema);
        int quotient = classified.Count(entry =>
            entry.Classification == SemanticCoverage.GrammarCoverageGate.AlphabetQuotient
        );
        int dtdDefault = classified.Count(entry =>
            entry.Classification == SemanticCoverage.GrammarCoverageGate.DtdDefault
        );
        Console.WriteLine($"  dead schema:       {dead}");
        Console.WriteLine($"  dtd default:       {dtdDefault}");
        Console.WriteLine($"  alphabet quotient: {quotient}");
        Console.WriteLine($"  awaiting fixture:  {classified.Count - dead - quotient - dtdDefault}");

        IReadOnlyList<SemanticCoverage.SurfaceEvidence> evidence = SemanticCoverage.GrammarCoverageGate.GradeEvidence(
            repositoryRoot,
            inventory
        );
        Console.WriteLine("evidence behind the covered surfaces:");
        foreach (
            SemanticCoverage.EvidenceStrength strength in Enum.GetValues<SemanticCoverage.EvidenceStrength>()
                .OrderByDescending(value => value)
        )
        {
            Console.WriteLine($"  {strength, -11} {evidence.Count(item => item.Strength == strength)}");
        }

        // Presence is not coverage, so an unwaived presence-only surface fails the run rather than
        // merely printing.
        string[] presenceOnly = evidence
            .Where(item => item.Strength == SemanticCoverage.EvidenceStrength.Presence)
            .Select(item => item.SurfaceId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        IReadOnlyList<string> waived = SemanticCoverage.GrammarCoverageGate.ReadPresenceWaivers(repositoryRoot);
        string[] unwaivedPresence = presenceOnly.Except(waived, StringComparer.Ordinal).ToArray();
        string[] staleWaivers = waived.Except(presenceOnly, StringComparer.Ordinal).ToArray();
        foreach (
            SemanticCoverage.SurfaceEvidence item in evidence
                .Where(item => item.Strength == SemanticCoverage.EvidenceStrength.Presence)
                .OrderBy(item => item.SurfaceId, StringComparer.Ordinal)
        )
        {
            string state = waived.Contains(item.SurfaceId, StringComparer.Ordinal) ? "waived" : "UNWAIVED";
            Console.Error.WriteLine($"PRESENCE ONLY ({state})  {item.SurfaceId}  [{item.FixtureId}] {item.Detail}");
        }

        foreach (string id in staleWaivers)
            Console.Error.WriteLine($"STALE PRESENCE WAIVER (delete it)  {id}");

        SemanticCoverage.AuditResult audit = SemanticCoverage.SemanticCoverageAudit.Run(
            inventory,
            catalog
        );
        Console.WriteLine(
            $"catalog audit: {(audit.IsComplete ? "complete" : $"{audit.Diagnostics.Count} diagnostic(s)")}"
        );
        foreach (SemanticCoverage.AuditDiagnostic diagnostic in audit.Diagnostics.Take(25))
            Console.Error.WriteLine($"AUDIT {diagnostic.Code}  {diagnostic.SubjectId}");
        if (audit.Diagnostics.Count > 25)
            Console.Error.WriteLine($"AUDIT ... and {audit.Diagnostics.Count - 25} more");

        IReadOnlyList<string> unbacked = SemanticCoverage.GrammarCoverageGate.UnbackedQuotients(
            classified,
            result.Covered
        );
        foreach (string id in unbacked)
            Console.Error.WriteLine($"UNBACKED QUOTIENT (no covered sibling)  {id}");

        var baseline = SemanticCoverage
            .GrammarCoverageGate.ReadBaseline(repositoryRoot)
            .Select(entry => entry.SurfaceId)
            .ToArray();
        string[] newGaps = result
            .Uncovered.Except(baseline, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] stale = baseline
            .Except(result.Uncovered, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        foreach (string id in newGaps)
            Console.Error.WriteLine($"NEW GAP    {id}");
        foreach (string id in stale)
            Console.Error.WriteLine($"NOW COVERED (delete from baseline)  {id}");
        // Writing happens BEFORE the verdict and never changes it: regenerating the baseline must
        // never absorb a real regression as a fresh todo line and report success.
        if (writeBaseline)
        {
            SemanticCoverage.GrammarCoverageGate.WriteBaseline(repositoryRoot, classified);
            Console.WriteLine($"wrote {SemanticCoverage.GrammarCoverageGate.BaselineRelativePath}");
        }

        if (
            !audit.IsComplete
            || newGaps.Length != 0
            || stale.Length != 0
            || unbacked.Count != 0
            || unwaivedPresence.Length != 0
            || staleWaivers.Length != 0
        )
        {
            Console.Error.WriteLine(
                $"semantic coverage findings: {newGaps.Length} new gap(s), "
                    + $"{stale.Length} stale line(s), {unbacked.Count} unbacked quotient(s), "
                    + $"{unwaivedPresence.Length} unwaived presence-only, {staleWaivers.Length} stale waiver(s)"
            );
            return 1;
        }

        Console.WriteLine("semantic coverage matches the baseline");
        return 0;
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

    // "Tracing" is the one construct the suite deliberately never covers: no adapter can produce a
    // trace through PROTOCOL.md's wire contract, so no word's expected.tsv signature can exercise it.
    private const string OutOfScopeConstruct = "Tracing (TraceType)";

    private static void RunCoverageReport(List<Fixture> fixtures, string fixturesRoot, string constructsPath)
    {
        string coverageCsvPath = Path.Combine(fixturesRoot, "coverage.csv");
        string rulesCsvPath = Path.Combine(fixturesRoot, "rules.csv");
        CoverageReport.CoverageResult result = CoverageReport.WriteCsvs(fixtures, coverageCsvPath, rulesCsvPath);

        Console.WriteLine();
        Console.WriteLine("coverage report");
        Console.WriteLine("===============");
        Console.WriteLine($"wrote {coverageCsvPath}");
        Console.WriteLine($"wrote {rulesCsvPath}");

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
              --semantic-coverage               Recompute generated-surface coverage and check the
                                                baseline; exits 1 on a new gap or a stale line.
              --write-coverage-baseline         Rewrite conformance/semantic-coverage-baseline.txt.
              --propose-semantic-catalog         Print a review-only catalog proposal to stdout;
                                                never changes conformance/semantic-catalog.yaml.
              --audited-source-scope <id>        Exact canonical C# symbol scope for a proposal;
                                                repeat for multiple scopes. Required when the
                                                checked-in catalog has no scopes.
              --counterfactual                  Re-run every fixture with each surface neutralized
                                                and check the ledger; exits 1 on a new, gone, or
                                                changed verdict.
              --write-counterfactual            Rewrite conformance/semantic-coverage-counterfactuals.tsv.
              --coverage-evidence               Combine the checked-in Surface ledger with a fresh
                                                Ordering adjacent-pair sweep into the 332-item
                                                Surface+Ordering inventory and print completeness by
                                                item kind and counterexample kind. Run
                                                --write-counterfactual first so the Surface half is
                                                fresh; this never recomputes it.
              --write-coverage-evidence         Also rewrite conformance/semantic-coverage-evidence.tsv.
              --rule-interaction-pairs          Recompute every fixture's pipeline-permitted Stratum
                                                rule-interaction pairs and check them against
                                                conformance/rule-interaction-pairs.tsv; exits 1 if stale.
              --write-rule-interaction-pairs    Rewrite conformance/rule-interaction-pairs.tsv.
              --interface-inventory             Recompute the DTD-declared IDREF/IDREFS interfaces,
                                                resolved against the real corpus, and check them
                                                against conformance/interface-inventory.tsv; exits 1
                                                if stale.
              --write-interface-inventory       Rewrite conformance/interface-inventory.tsv.
              --interaction-chains              Recompute the write/read interaction-chain denominator
                                                (writer edge x payload type x reader edge, at each
                                                interface-inventory junction) and check it against
                                                conformance/interaction-chains.tsv; exits 1 if stale.
              --write-interaction-chains        Rewrite conformance/interaction-chains.tsv.
              --dataflow-obligations            Recompute the data-flow/MC/DC obligation-matrix cells
                                                (all-uses, plus MC/DC on every gate, plus every
                                                kill-path witnessed) from
                                                conformance/interaction-chains.tsv and check them
                                                against conformance/dataflow-obligations.tsv; exits 1
                                                if stale.
              --write-dataflow-obligations      Rewrite conformance/dataflow-obligations.tsv.
              --coverage-traceability           Re-sweep interface severance witness (expensive -- one
                                                reparse per present interface x fixture) and check
                                                interface-witness.tsv, grammar-coverage-ledger.tsv,
                                                construct-claim-corroboration.tsv, and
                                                fold-in-candidates.tsv; exits 1 if any is stale.
              --write-coverage-traceability     Rewrite all four files above.
              --evidence-cards                  Render one reviewable Markdown card per
                                                conformance/dataflow-obligations.tsv cell under
                                                conformance/evidence-cards/ and check it against what
                                                is checked in; exits 1 if stale. Never recomputes any
                                                ledger -- purely a rendering of already-checked-in
                                                facts for a human or reviewing agent to read.
              --write-evidence-cards            Rewrite conformance/evidence-cards/.
              --engine-gate-inventory           Recompute the FailureReason-keyed engine-gate inventory
                                                (mechanically scanned raise sites + a traced engine
                                                sweep for witness evidence) and check it against
                                                conformance/engine-gate-inventory.tsv; exits 1 if stale.
              --write-engine-gate-inventory     Rewrite conformance/engine-gate-inventory.tsv.
              --gate-obligations                Recompute the gate-keyed MC/DC obligation ledger
                                                (Blocked/Control arm per FailureReason gate) from
                                                engine-gate-inventory.tsv, interface-witness.tsv, and
                                                fieldworks-producibility.tsv (falling back to a fresh
                                                severance run only where those do not already cover an
                                                attribute) and check it against
                                                conformance/gate-obligations.tsv; exits 1 if stale.
              --write-gate-obligations          Rewrite conformance/gate-obligations.tsv.
              --repository-root <path>          Repository root for the flags above.
                                                (default: <fixtures>/constructs.txt).
              --propose                        Self-check only: on a signature mismatch, print
                                                the words.yaml patch that would reconcile it. Never
                                                writes any file.
            """
        );
    }
}
