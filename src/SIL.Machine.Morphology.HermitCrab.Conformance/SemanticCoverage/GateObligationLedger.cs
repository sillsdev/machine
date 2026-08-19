#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Whether a gate obligation's arm has a real, checked witness -- never a guess.</summary>
public enum GateArmStatus
{
    Evidenced,
    NotEvidenced,
}

/// <summary>
/// The checked-in denominator for MC/DC obligations keyed to <see cref="FailureReason"/> -- HermitCrab's
/// OWN 23 engine decision points (<see cref="EngineGateInventoryLedger"/>) -- rather than to a DTD
/// attribute pair (<see cref="DataflowObligationLedger"/>).
///
/// <para>
/// <b>Why this ledger, and why it is primary now.</b> <see cref="DataflowObligationLedger"/> enumerates
/// four MC/DC arms per writer/reader chain but can only ever CERTIFY one of them
/// (<c>FindPairedWitness</c> derives the satisfied arm from the reader attribute's own name:
/// <c>required*</c> -&gt; AbsentGatedForm, <c>excluded*</c> -&gt; PresentGatedForm, anything else -&gt;
/// null) -- of 346 enumerated cells, 28 are even certifiable. Worse, it cannot NAME a gate the DTD fuses
/// into no single attribute at all: six of the 23 <see cref="FailureReason"/> values
/// (<see cref="EngineGateInventoryLedger.DtdAttributes"/>) have no row there whatsoever. This ledger
/// starts from the ENGINE's own 23 gates instead, so every one of them gets a denominator row regardless
/// of whether the schema names it with an attribute, and MC/DC's two arms per gate are evidenced with
/// the engine's own vocabulary:
/// <list type="bullet">
/// <item><b>Blocked</b> -- a word that fails, and HermitCrab's own trace tree NAMES this exact
/// <see cref="FailureReason"/> somewhere in that failure, and severing the construct that feeds the
/// gate (<see cref="InterfaceWitnessGate.Sever"/>) flips that SAME word to a successful parse. This is
/// stronger evidence than <see cref="DataflowObligationLedger"/>'s chain pairing: the trace attributes
/// the failure to the gate directly, rather than inferring an arm from an attribute's spelling.</item>
/// <item><b>Control</b> -- a word (in the same fixture) where the SAME grammar rule instance that fed
/// the Blocked arm's gate fires in a SUCCESSFUL parse, proving the rule can apply at all and that the
/// Blocked arm's failure is attributable to the gate rather than to a rule that never runs. Resolvable
/// when the gated construct sits directly on a rule element <see cref="GrammarRuleIndex"/> can name
/// (<c>MorphologicalRule</c>/<c>CompoundingRule</c>/<c>RealizationalRule</c>/<c>PhonologicalRule</c>/
/// <c>MetathesisRule</c>), or on one of four child element kinds <see
/// cref="GrammarRuleIndex.ResolveAncestorRuleId"/> can walk up to a rule ancestor from
/// (<c>MorphologicalInput</c>, <c>PhonologicalSubrule</c> always resolve this way; <c>Allomorph</c>
/// and <c>AffixTemplate</c> never do -- neither ever has a rule ancestor, see that method's own doc
/// comment). A co-occurrence or bare slot construct has no "Applied" trace event to check against at
/// all (the same documented gap <see cref="FailureRuleAttributor"/> already records for allomorph
/// identity), and is reported <see cref="GateArmStatus.NotEvidenced"/> naming exactly that limitation,
/// never guessed.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Every obligation also carries its two layer verdicts, so a miss says WHERE it is blocked.</b>
/// <c>xml_reachable</c> is whether any DTD attribute or element reaches the gate at all (from
/// <see cref="EngineGateInventoryLedger.DtdAttributes"/>; five of its six <c>"-"</c> entries are reachable
/// only via element content -- <see cref="ElementContentReachableWithoutAttribute"/> -- and one,
/// <see cref="FailureReason.SurfaceFormMismatch"/>, is a pure engine-internal check no grammar construct
/// feeds at all). <c>flex_producible</c> is FieldWorks' own HCLoader capability
/// (<c>conformance/fieldworks-producibility.tsv</c>, treated as fixed -- see that file's own doc
/// comment). An obligation is <c>worth_covering</c> only when both are Yes: 21 of the 23 gates today
/// (<see cref="FailureReason.SurfaceFormMismatch"/> fails the first, <see
/// cref="FailureReason.ObligatorySyntacticFeatures"/> the second). A gate that IS worth covering can
/// still be unevidenced for two further, honestly distinct reasons: the current corpus never witnesses
/// it at all (<see cref="EngineGateStatus.Unreached"/>), or it is witnessed but has no DTD attribute to
/// sever (the three <c>"-"</c> gates that ARE reachable and producible -- <see
/// cref="FailureReason.Environments"/>, <see cref="FailureReason.DisjunctiveAllomorph"/>, <see
/// cref="FailureReason.Pattern"/> -- for which this ledger has no isolable element-content severance
/// primitive yet). Every one of these is named in the row's own evidence text, never collapsed to a
/// single "Unknown".
/// </para>
///
/// <para>
/// <b><see cref="DataflowObligationLedger"/> is kept, not superseded.</b> It answers a genuinely
/// different question this ledger does not: whether a specific WRITER/READER PAYLOAD PAIR survives the
/// full MC/DC treatment (n-condition vectors, the four mutator-kill classes) for the 40 write/read
/// chains <see cref="InteractionChainLedger"/> derives from the DTD. This ledger answers whether each of
/// the engine's 23 OWN DECISION POINTS is independently shown to matter.
/// </para>
/// </summary>
public static class GateObligationLedger
{
    public const string RelativePath = "conformance/gate-obligations.tsv";

    private const int ColumnCount = 9;
    private const string NoValue = "-";
    private const string BlockedArm = "Blocked";
    private const string ControlArm = "Control";

    public sealed record Row(
        string Gate,
        string Arm, // "Blocked" | "Control"
        string XmlReachable, // "Yes" | "No"
        string FlexProducible, // "Yes" | "No"
        string WorthCovering, // "Yes" | "No" -- derived: XmlReachable == Yes && FlexProducible == Yes
        GateArmStatus Status,
        string Fixture,
        string Word,
        string Evidence
    );

    /// <summary>
    /// The five of <see cref="EngineGateInventoryLedger.DtdAttributes"/>'s six <c>"-"</c> gates that are
    /// still reachable from a fixture -- via child ELEMENTS (<see cref="FailureReason.Environments"/>),
    /// PhoneticSequence element CONTENT (<see cref="FailureReason.Pattern"/>/<see
    /// cref="FailureReason.HeadPattern"/>/<see cref="FailureReason.NonHeadPattern"/>), or allomorph
    /// sibling structure (<see cref="FailureReason.DisjunctiveAllomorph"/>) -- as opposed to <see
    /// cref="FailureReason.SurfaceFormMismatch"/>, a pure engine-internal reconstruction check with no
    /// grammar input at all. Deliberately a fixed set, not re-derived: which of these five have an
    /// isolable severance primitive built (three do not yet -- see this class's own doc comment) is a
    /// tooling question, not something a fixture sweep can answer for us.
    /// </summary>
    private static readonly HashSet<string> ElementContentReachableWithoutAttribute = new(StringComparer.Ordinal)
    {
        "Environments",
        "DisjunctiveAllomorph",
        "Pattern",
        "HeadPattern",
        "NonHeadPattern",
    };

    /// <summary>Rule elements that carry their own <c>id</c> attribute directly -- <see
    /// cref="GrammarRuleIndex"/> needs no ancestor walk to name the rule these belong to.</summary>
    private static readonly HashSet<string> RuleIndexedElements = new(StringComparer.Ordinal)
    {
        "MorphologicalRule",
        "CompoundingRule",
        "RealizationalRule",
        "PhonologicalRule",
        "MetathesisRule",
    };

    /// <summary>Elements with no rule id of their own that <see cref="GrammarRuleIndex.ResolveAncestorRuleId"/>
    /// can still name by walking up to the nearest rule-element ancestor. Not every instance resolves: an
    /// <c>Allomorph</c> is always a child of <c>LexicalEntry</c>, never of a rule, and an <c>AffixTemplate</c> has no
    /// <c>id</c> attribute of its own (the DTD never declares one) and sits under <c>Stratum</c>, never
    /// under a rule either -- both genuinely have no rule to attribute a Control arm to.</summary>
    private static readonly HashSet<string> AncestorResolvableElements = new(StringComparer.Ordinal)
    {
        "Allomorph",
        "MorphologicalInput",
        "AffixTemplate",
        "PhonologicalSubrule",
    };

    private static bool IsXmlReachable(EngineGateInventoryLedger.Row gateRow) =>
        gateRow.DtdAttributes != NoValue || ElementContentReachableWithoutAttribute.Contains(gateRow.Gate);

    private static string XmlUnreachableReason(string gate) =>
        gate == nameof(FailureReason.SurfaceFormMismatch)
            ? "pure engine-internal reconstruction check (Morpher.cs's confirming-synthesis IsMatch) with no grammar attribute or element input at all -- see EngineGateInventoryLedger.DtdAttributes"
            : "no DTD attribute, and not one of the five known element-content-reachable gates -- see EngineGateInventoryLedger.DtdAttributes";

    private static string FlexUnproducibleReason(string gate) =>
        gate == nameof(FailureReason.ObligatorySyntacticFeatures)
            ? "FieldWorks' HCLoader never sets AffixProcessRule/CompoundingRule.ObligatorySyntacticFeatures for any construct FLEx exposes -- conformance/fieldworks-producibility.tsv"
            : "conformance/fieldworks-producibility.tsv records this construct as not producible by FieldWorks' HCLoader";

    /// <summary>failure-reason subject -> producible == "Yes", read straight from the hand-verified,
    /// checked-in ledger this class treats as fixed (see that file's own doc comment).</summary>
    private static IReadOnlyDictionary<string, bool> ReadFlexProducibility(string repositoryRoot)
    {
        string path = Path.Combine(repositoryRoot, "conformance", "fieldworks-producibility.tsv");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                "conformance/fieldworks-producibility.tsv is missing; GateObligationLedger needs it to compute flex_producible"
            );
        }

        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (
                line.Length == 0
                || line.StartsWith("#", StringComparison.Ordinal)
                || line.StartsWith("subject_kind\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields.Length < 3 || fields[0] != "failure-reason")
                continue;
            result[fields[1]] = fields[2] == "Yes";
        }
        return result;
    }

    // ----------------------------------------------------------------------------------------
    // Per-fixture baseline: one traced parse per word, shared by every gate that touches this
    // fixture, so a corpus-wide sweep costs one traced pass per fixture, not one per (gate, fixture).
    // ----------------------------------------------------------------------------------------

    private sealed record WordBaseline(bool Succeeded, IReadOnlySet<string> ObservedGates, IReadOnlySet<string> FiredRuleIds);

    private static void WalkFailureReasons(Trace node, HashSet<string> gates)
    {
        if (node.FailureReason != FailureReason.None)
            gates.Add(node.FailureReason.ToString());
        foreach (Trace child in node.Children)
            WalkFailureReasons(child, gates);
    }

    private static IReadOnlyDictionary<string, WordBaseline> BuildWordBaselines(Fixture fixture)
    {
        var byWord = new Dictionary<string, WordBaseline>(StringComparer.Ordinal);

        Language language;
        try
        {
            language = XmlLanguageLoader.Load(fixture.GrammarPath);
        }
        catch
        {
            return byWord;
        }

        GrammarRuleIndex ruleIndex = GrammarRuleIndex.Load(fixture.GrammarPath);
        var morpher = new Morpher(new TraceManager { IsTracing = true }, language);

        foreach (WordEntry entry in fixture.Words.Words)
        {
            bool guessRoot = entry.Parses.Any(p => p.Guess);
            List<Word> results;
            object? trace;
            try
            {
                results = morpher.ParseWord(entry.Word, out trace, guessRoot).ToList();
            }
            catch
            {
                // Mirrors EngineGateWitnessSweep: a word the engine cannot even trace evidences nothing.
                continue;
            }

            var observedGates = new HashSet<string>(StringComparer.Ordinal);
            if (trace is Trace root)
                WalkFailureReasons(root, observedGates);

            var firedRuleIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (Word result in results)
            {
                foreach (string id in TraceRuleAttributor.MorphologicalRuleIds(result, ruleIndex))
                    firedRuleIds.Add(id);
            }
            if (trace is not null)
            {
                foreach (string id in TraceRuleAttributor.WordLevelRuleIds(trace, ruleIndex))
                    firedRuleIds.Add(id);
            }

            byWord[entry.Word] = new WordBaseline(results.Count > 0, observedGates, firedRuleIds);
        }

        return byWord;
    }

    // ----------------------------------------------------------------------------------------
    // Compute
    // ----------------------------------------------------------------------------------------

    public static IReadOnlyList<Row> Compute(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);

        IReadOnlyList<EngineGateInventoryLedger.Row> gateRows = EngineGateInventoryLedger.Read(repositoryRoot);
        if (gateRows.Count == 0)
        {
            throw new InvalidOperationException(
                $"{EngineGateInventoryLedger.RelativePath} is missing or empty; regenerate it first with --write-engine-gate-inventory"
            );
        }

        IReadOnlyDictionary<string, bool> flexProducible = ReadFlexProducibility(repositoryRoot);

        Dictionary<string, Fixture> fixturesById = Fixture
            .DiscoverAll(Path.Combine(repositoryRoot, "conformance"))
            .ToDictionary(f => f.Id, StringComparer.Ordinal);

        var witnessByKey = new Dictionary<(string Element, string Attribute, string Fixture), InterfaceWitnessResult>();
        foreach (InterfaceWitnessResult w in InterfaceWitnessLedger.Read(repositoryRoot))
            witnessByKey[(w.Element, w.Attribute, w.FixtureId)] = w;

        SemanticInventory? inventoryCache = null;
        SemanticInventory GetInventory() => inventoryCache ??= GrammarCoverageGate.ReadInventory(repositoryRoot);

        var wordBaselineCache = new Dictionary<string, IReadOnlyDictionary<string, WordBaseline>>(StringComparer.Ordinal);
        var plainBaselineCache = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        string scratch = Path.Combine(Path.GetTempPath(), "hc-gate-obligations");

        IReadOnlyDictionary<string, WordBaseline> GetWordBaselines(string fixtureId)
        {
            if (wordBaselineCache.TryGetValue(fixtureId, out IReadOnlyDictionary<string, WordBaseline>? cached))
                return cached;
            cached = fixturesById.TryGetValue(fixtureId, out Fixture? fx)
                ? BuildWordBaselines(fx)
                : new Dictionary<string, WordBaseline>(StringComparer.Ordinal);
            wordBaselineCache[fixtureId] = cached;
            return cached;
        }

        // Reuses conformance/interface-witness.tsv's already-computed severance where it exists (every
        // IDREF/IDREFS attribute); falls back to a fresh InterfaceWitnessGate.Evaluate run only for the
        // boolean/non-IDREF attributes that ledger's own scope (InterfaceInventoryLedger) never covers
        // (Allomorph.isBound, Slot.optional, AffixTemplate.final, MorphologicalRule.partial,
        // *.multipleApplication).
        InterfaceWitnessResult? GetSeveranceResult(string element, string attribute, string fixtureId)
        {
            if (witnessByKey.TryGetValue((element, attribute, fixtureId), out InterfaceWitnessResult? cached))
                return cached;
            if (!fixturesById.TryGetValue(fixtureId, out Fixture? fixture))
                return null;

            if (!plainBaselineCache.TryGetValue(fixtureId, out IReadOnlyList<string>? cachedBaseline))
            {
                try
                {
                    cachedBaseline = CounterfactualGate.EvaluateWithTimeout(
                        fixture.GrammarPath,
                        fixture.Words.Words.Select(w => w.Word).ToArray()
                    );
                }
                catch (Exception)
                {
                    // A fixture whose own unmutated baseline cannot be evaluated evidences nothing for
                    // any gate; the caller's loop simply moves on to the next fixture/attribute.
                    return null;
                }
                plainBaselineCache[fixtureId] = cachedBaseline;
            }

            return InterfaceWitnessGate.Evaluate(fixture, element, attribute, GetInventory(), cachedBaseline!, scratch);
        }

        var rows = new List<Row>();
        foreach (FailureReason reason in Enum.GetValues<FailureReason>())
        {
            if (reason == FailureReason.None)
                continue;

            string gate = reason.ToString();
            EngineGateInventoryLedger.Row gateRow =
                gateRows.FirstOrDefault(r => r.Gate == gate)
                ?? throw new InvalidOperationException($"{EngineGateInventoryLedger.RelativePath} has no row for {gate}");

            bool xmlReachable = IsXmlReachable(gateRow);
            bool flex = flexProducible.TryGetValue(gate, out bool producible) && producible;
            bool worthCovering = xmlReachable && flex;

            (ArmResult blocked, ArmResult control) = EvaluateGate(
                gateRow,
                xmlReachable,
                worthCovering,
                GetWordBaselines,
                GetSeveranceResult,
                fixturesById
            );

            rows.Add(
                new Row(
                    gate,
                    BlockedArm,
                    YesNo(xmlReachable),
                    YesNo(flex),
                    YesNo(worthCovering),
                    blocked.Status,
                    blocked.Fixture,
                    blocked.Word,
                    blocked.Evidence
                )
            );
            rows.Add(
                new Row(
                    gate,
                    ControlArm,
                    YesNo(xmlReachable),
                    YesNo(flex),
                    YesNo(worthCovering),
                    control.Status,
                    control.Fixture,
                    control.Word,
                    control.Evidence
                )
            );
        }

        return Sort(rows);
    }

    private static string YesNo(bool value) => value ? "Yes" : "No";

    private sealed record ArmResult(GateArmStatus Status, string Fixture, string Word, string Evidence);

    private static ArmResult NotEvidencedArm(string reason) => new(GateArmStatus.NotEvidenced, NoValue, NoValue, reason);

    private static (ArmResult Blocked, ArmResult Control) BothNotEvidenced(string reason) =>
        (NotEvidencedArm(reason), NotEvidencedArm(reason));

    private static (ArmResult Blocked, ArmResult Control) EvaluateGate(
        EngineGateInventoryLedger.Row gateRow,
        bool xmlReachable,
        bool worthCovering,
        Func<string, IReadOnlyDictionary<string, WordBaseline>> getWordBaselines,
        Func<string, string, string, InterfaceWitnessResult?> getSeverance,
        IReadOnlyDictionary<string, Fixture> fixturesById
    )
    {
        if (!worthCovering)
        {
            return BothNotEvidenced(
                !xmlReachable
                    ? $"not worth covering: xml_reachable=No -- {XmlUnreachableReason(gateRow.Gate)}"
                    : $"not worth covering: flex_producible=No -- {FlexUnproducibleReason(gateRow.Gate)}"
            );
        }

        if (gateRow.Status == EngineGateStatus.Unreached)
        {
            return BothNotEvidenced(
                "gate never witnessed by any fixture in the current corpus (conformance/engine-gate-inventory.tsv: status=Unreached) -- no candidate word exists to evidence either arm"
            );
        }

        if (gateRow.DtdAttributes == NoValue)
        {
            return BothNotEvidenced(
                "xml_reachable=Yes via element content, not an attribute -- no isolable severance primitive exists in this ledger for a bare element-content construct yet (conformance/docs/how-it-is-computed.md's gate-obligations section)"
            );
        }

        (string Element, string Attribute)[] attributePairs = gateRow
            .DtdAttributes.Split(';')
            .Select(pair => pair.Split('.'))
            .Where(parts => parts.Length == 2)
            .Select(parts => (parts[0], parts[1]))
            .ToArray();

        string[] fixtureIds =
            gateRow.TriggeredByFixtures == NoValue ? Array.Empty<string>() : gateRow.TriggeredByFixtures.Split(';');

        foreach (string fixtureId in fixtureIds)
        {
            IReadOnlyDictionary<string, WordBaseline> baselines = getWordBaselines(fixtureId);
            foreach ((string element, string attribute) in attributePairs)
            {
                InterfaceWitnessResult? severance = getSeverance(element, attribute, fixtureId);
                if (severance is null || severance.Verdict != CounterfactualVerdict.Evidenced)
                    continue;
                if (!DataflowObligationLedger.IsFailOutcome(severance.ExampleOutcome))
                    continue;
                if (DataflowObligationLedger.IsFailOutcome(severance.CounterexampleOutcome))
                    continue;

                string? candidateWord = severance.ExampleWord;
                if (
                    candidateWord is null
                    || !baselines.TryGetValue(candidateWord, out WordBaseline? wb)
                    || wb.Succeeded
                    || !wb.ObservedGates.Contains(gateRow.Gate)
                )
                {
                    continue;
                }

                var blocked = new ArmResult(
                    GateArmStatus.Evidenced,
                    fixtureId,
                    candidateWord,
                    $"severing {element}.{attribute} in {fixtureId} flips '{candidateWord}' from "
                        + $"{severance.ExampleOutcome} to {severance.CounterexampleOutcome} (conformance/interface-witness.tsv "
                        + $"or a fresh equivalent severance run), and '{candidateWord}''s own baseline trace names "
                        + $"{gateRow.Gate} directly (this ledger's own traced sweep)"
                );
                ArmResult control = EvaluateControl(element, attribute, fixtureId, fixturesById, baselines);
                return (blocked, control);
            }
        }

        string attributeList = string.Join(", ", attributePairs.Select(p => $"{p.Element}.{p.Attribute}"));
        string fixtureList = string.Join(", ", fixtureIds);
        string blockedReason =
            fixtureIds.Length == 0
                ? "no triggering fixture recorded for this gate in conformance/engine-gate-inventory.tsv"
                : $"no severance of {{{attributeList}}} across {{{fixtureList}}} produced a same-word fail-to-pass flip "
                    + $"whose baseline trace also names {gateRow.Gate} -- the gate may only fire as noise against a "
                    + "candidate unrelated to the word's final result (see FailureRuleAttributor's own doc comment)";
        return (
            NotEvidencedArm(blockedReason),
            NotEvidencedArm(
                "Blocked arm unevidenced; Control arm requires a confirmed blocking instance to identify which rule to check"
            )
        );
    }

    private static ArmResult EvaluateControl(
        string element,
        string attribute,
        string fixtureId,
        IReadOnlyDictionary<string, Fixture> fixturesById,
        IReadOnlyDictionary<string, WordBaseline> baselines
    )
    {
        bool ancestorResolvable = AncestorResolvableElements.Contains(element);
        if (!RuleIndexedElements.Contains(element) && !ancestorResolvable)
        {
            return NotEvidencedArm(
                $"{element} is not a rule element GrammarRuleIndex resolves to a fired-rule id, directly or by "
                    + "ancestor -- co-occurrence and template/slot constructs have no \"Applied\" trace event to "
                    + "check against (the same gap FailureRuleAttributor's own doc comment records for allomorph identity)"
            );
        }

        if (!fixturesById.TryGetValue(fixtureId, out Fixture? fixture))
            return NotEvidencedArm($"fixture {fixtureId} could not be reloaded to resolve a rule id");

        string? ruleId = null;
        try
        {
            XDocument doc = XDocument.Load(fixture.GrammarPath);
            XElement? instance = doc.Descendants(element)
                .FirstOrDefault(e => !string.IsNullOrEmpty((string?)e.Attribute(attribute)));
            if (instance is not null)
            {
                ruleId = ancestorResolvable
                    ? GrammarRuleIndex.ResolveAncestorRuleId(instance)
                    : (string?)instance.Attribute("id");
            }
        }
        catch
        {
            // ruleId stays null; handled by the check below.
        }

        if (ruleId is null)
        {
            string reason = ancestorResolvable
                ? $"the {element} instance carrying {attribute} in {fixtureId} has no rule-element ancestor "
                    + "GrammarRuleIndex can resolve (conformance/docs/severance-mechanics.md) -- it sits outside "
                    + "any rule, so no rule id exists to attribute a Control arm to"
                : $"the {element} instance carrying {attribute} in {fixtureId} has no XML id attribute; rule-id attribution not possible";
            return NotEvidencedArm(reason);
        }

        foreach ((string word, WordBaseline wb) in baselines)
        {
            if (wb.Succeeded && wb.FiredRuleIds.Contains(ruleId))
            {
                return new ArmResult(
                    GateArmStatus.Evidenced,
                    fixtureId,
                    word,
                    $"rule '{ruleId}' ({element}) also fires in a successful parse of '{word}' in {fixtureId} "
                        + "(TraceRuleAttributor over this ledger's own baseline sweep), proving it can apply outside the blocked condition"
                );
            }
        }

        return NotEvidencedArm(
            $"rule '{ruleId}' ({element}) never fires in any successful parse within {fixtureId} -- cannot confirm the rule applies at all outside the blocked condition"
        );
    }

    private static IReadOnlyList<Row> Sort(IEnumerable<Row> rows) =>
        rows.OrderBy(r => r.Gate, StringComparer.Ordinal).ThenBy(r => r.Arm, StringComparer.Ordinal).ToArray();

    public static void Write(string repositoryRoot, IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(rows);
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(path, ToText(rows));
    }

    public static string ToText(IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var writer = new StringWriter();
        writer.WriteLine(
            "# GENERATED by hc-conformance --write-gate-obligations. One row per (gate, arm) pair, gate"
        );
        writer.WriteLine(
            "# ranging over all 23 SIL.Machine.Morphology.HermitCrab.FailureReason members (excluding None)"
        );
        writer.WriteLine(
            "# and arm in {Blocked, Control} -- the two arms MC/DC demands for each of the engine's own"
        );
        writer.WriteLine(
            "# decision points, as opposed to conformance/dataflow-obligations.tsv's DTD-attribute-pair"
        );
        writer.WriteLine(
            "# denominator (kept, not superseded -- see conformance/docs/how-it-is-computed.md). xml_reachable"
        );
        writer.WriteLine(
            "# and flex_producible are the two layer verdicts a gate must clear to be worth_covering;"
        );
        writer.WriteLine(
            "# status is Evidenced only for a same-word severance flip whose baseline trace names the gate"
        );
        writer.WriteLine(
            "# directly (Blocked) or a same-fixture rule-id match proving the gated rule fires successfully"
        );
        writer.WriteLine(
            "# elsewhere (Control); evidence always names which layer or mechanism blocks an unevidenced cell"
        );
        writer.WriteLine("# rather than collapsing to a bare \"Unknown\".");
        writer.WriteLine(
            "gate\tarm\txml_reachable\tflex_producible\tworth_covering\tstatus\tfixture\tword\tevidence"
        );
        foreach (Row row in Sort(rows))
        {
            writer.WriteLine(
                string.Join(
                    '\t',
                    row.Gate,
                    row.Arm,
                    row.XmlReachable,
                    row.FlexProducible,
                    row.WorthCovering,
                    row.Status,
                    row.Fixture,
                    row.Word,
                    row.Evidence
                )
            );
        }
        return writer.ToString();
    }

    /// <summary>Reads the checked-in ledger, or an empty list if it has never been written.</summary>
    public static IReadOnlyList<Row> Read(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            return Array.Empty<Row>();

        var rows = new List<Row>();
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (
                line.Length == 0
                || line.StartsWith("#", StringComparison.Ordinal)
                || line.StartsWith("gate\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields.Length != ColumnCount)
                throw new FormatException($"{RelativePath}: '{line}' must be {ColumnCount} tab-separated fields");
            if (!Enum.TryParse(fields[5], out GateArmStatus status))
                throw new FormatException($"{RelativePath}: unknown status '{fields[5]}'");

            rows.Add(
                new Row(fields[0], fields[1], fields[2], fields[3], fields[4], status, fields[6], fields[7], fields[8])
            );
        }

        return rows;
    }
}
