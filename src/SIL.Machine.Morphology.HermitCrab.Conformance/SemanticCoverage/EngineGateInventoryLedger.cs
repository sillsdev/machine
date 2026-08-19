#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Whether at least one fixture word demonstrably makes a gate fire.</summary>
public enum EngineGateStatus
{
    Witnessed,
    Unreached,
}

/// <summary>
/// The checked-in denominator for a coverage layer keyed to <see cref="FailureReason"/> -- HermitCrab's
/// OWN enumeration of the 23 (excluding <see cref="FailureReason.None"/>) decisions it makes when
/// declining to apply or unapply something -- rather than to a DTD attribute pair, which is what every
/// other ledger in this directory (<see cref="DataflowObligationLedger"/>, <see cref="InteractionChainLedger"/>,
/// <see cref="InterfaceInventoryLedger"/>) is keyed to.
///
/// <para>
/// <b>Why this denominator, alongside the DTD-attribute one, not instead of it.</b> The DTD-attribute
/// layer is authored from the SCHEMA outward: it enumerates writer/reader attribute pairs a grammar
/// author can set. That is the right question for "does severing this payload flip a parse", but it
/// cannot even NAME a decision the schema fuses into no single attribute, or that several attributes
/// share. <see cref="FailureReason"/> is authored from the ENGINE outward, so it draws different lines:
/// <see cref="FailureReason.PartialParse"/> is one gate reached from four call sites across three files
/// (an unfinished obligatory template slot, an unrealizable realizational feature struct, or a
/// still-final-template-shaped derivation with rules left in its stratum) that no DTD-attribute pair
/// names at all; <see cref="FailureReason.BoundRoot"/>, <see cref="FailureReason.MaxApplicationCount"/>,
/// <see cref="FailureReason.DisjunctiveAllomorph"/>, and <see cref="FailureReason.SurfaceFormMismatch"/>
/// likewise have no row in <c>conformance/dataflow-obligations.tsv</c> at all -- not because they are
/// unimportant, but because that ledger's cells are payload write/read PAIRS and these are not that
/// shape. See this class's own <see cref="DtdAttributes"/> table for exactly which of the 23 turn out
/// to have an identifiable attribute anyway (more than half do) and which genuinely do not.
/// </para>
///
/// <para>
/// <b>A <see cref="EngineGateStatus.Witnessed"/> row here is a WEAKER claim than a
/// <see cref="ObligationStatus.Satisfied"/> dataflow-obligation cell -- do not read the two as
/// comparable strength.</b> A satisfied obligation cell means a same-word PAIR witness: severing the
/// writer AND severing the reader both flip ONE named word from failed to successful (see
/// <see cref="DataflowObligationLedger"/>'s own doc comment). A <c>Witnessed</c> row here means only
/// that <see cref="EngineGateWitnessSweep"/> observed this <see cref="FailureReason"/> value SOMEWHERE
/// in SOME word's trace tree -- with no attempt to distinguish a genuine per-word conflict from a rule
/// merely tried against a candidate it has nothing to do with, which
/// <see cref="FailureRuleAttributor"/>'s own doc comment documents as routine for several of these
/// reasons. "Some word made this gate fire" and "severing a payload provably flipped a parse" are
/// different claims; this ledger only ever makes the first one.
/// </para>
/// </summary>
public static class EngineGateInventoryLedger
{
    public const string RelativePath = "conformance/engine-gate-inventory.tsv";

    private const int ColumnCount = 6;
    private const string NoAttributes = "-";

    public sealed record Row(
        string Gate,
        string RaiseSites,
        string DtdAttributes,
        string TriggeredByFixtures,
        string TriggeringWords,
        EngineGateStatus Status
    );

    /// <summary>
    /// The DTD attribute(s) that, per <c>src/SIL.Machine.Morphology.HermitCrab/XmlLanguageLoader.cs</c>,
    /// feed the runtime state a gate's raise sites check -- hand-verified against source, exactly like
    /// <see cref="DataflowObligationLedger.ApplicableMutatorClasses"/>'s own schema/engine-derived
    /// mapping, because "which attribute reaches which engine decision" requires reading what the
    /// loader DOES with an attribute, which no regex over the DTD or the engine alone can derive.
    /// <see cref="NoAttributes"/> is a genuine finding, not a placeholder, for six of the 23: three
    /// (<see cref="FailureReason.Pattern"/>/<see cref="FailureReason.HeadPattern"/>/
    /// <see cref="FailureReason.NonHeadPattern"/>) are driven by PhoneticSequence ELEMENT CONTENT (the
    /// shape itself), never an attribute; <see cref="FailureReason.Environments"/> and
    /// <see cref="FailureReason.DisjunctiveAllomorph"/> are driven by the presence/order/overlap of
    /// RequiredEnvironments/ExcludedEnvironments child ELEMENTS among an allomorph's siblings, again no
    /// attribute; <see cref="FailureReason.SurfaceFormMismatch"/> is a pure engine-internal
    /// reconstruction check with no grammar input at all. Every other gate DOES resolve to a real
    /// IDREFS/boolean attribute -- including three that are easy to assume have none
    /// (<see cref="FailureReason.BoundRoot"/> -> <c>Allomorph.isBound</c>,
    /// <see cref="FailureReason.MaxApplicationCount"/> -> <c>MorphologicalRule.multipleApplication</c>,
    /// and <see cref="FailureReason.PartialParse"/> -> <c>Slot.optional</c>/<c>AffixTemplate.final</c>).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> DtdAttributes = new Dictionary<string, string>(
        StringComparer.Ordinal
    )
    {
        // XmlLanguageLoader.cs:897 (MorphologicalRule), :1227 (CompoundingRule).
        ["ObligatorySyntacticFeatures"] = "MorphologicalRule.outputObligatoryFeatures;CompoundingRule.outputObligatoryFeatures",
        // AllomorphCoOccurrenceRule is a global rule element that names its target via an IDREF
        // attribute on ITSELF (conformance/HermitCrabInput.dtd:601-606), not an attribute on Allomorph.
        ["AllomorphCoOccurrenceRules"] = "AllomorphCoOccurrenceRule.primaryAllomorph",
        // RequiredEnvironments/ExcludedEnvironments are child ELEMENTS of Allomorph
        // (conformance/HermitCrabInput.dtd:305,320-322), never an attribute.
        ["Environments"] = NoAttributes,
        // Same shape as AllomorphCoOccurrenceRules: MorphemeCoOccurrenceRule names its target via its
        // own IDREF attribute (conformance/HermitCrabInput.dtd:585-590).
        ["MorphemeCoOccurrenceRules"] = "MorphemeCoOccurrenceRule.primaryMorpheme",
        // Structural: which of a morpheme's OTHER allomorphs are tried, driven by allomorph order,
        // free-fluctuation, and Environment overlap (Allomorph.cs's IsWordValid) -- no attribute names it.
        ["DisjunctiveAllomorph"] = NoAttributes,
        // Pure engine-internal reconstruction check (Morpher.cs's IsMatch): the confirming synthesis's
        // shape does not match the original surface. No grammar attribute feeds this at all.
        ["SurfaceFormMismatch"] = NoAttributes,
        // Driven by whether a MorphologicalInput/HeadMorphologicalInput/NonHeadMorphologicalInput's
        // PhoneticSequence CONTENT matches the input shape -- element content, not an attribute.
        ["Pattern"] = NoAttributes,
        ["HeadPattern"] = NoAttributes,
        ["NonHeadPattern"] = NoAttributes,
        // XmlLanguageLoader.cs:872-874 (MorphologicalRule), :788-796 (PhonologicalSubrule). The
        // RealizationalRule and per-subrule/per-allomorph raise sites reach this same reason from
        // RequiredHeadFeatures/RequiredFootFeatures ELEMENTS alone, with no attribute at all -- these
        // two attributes are real but do not cover every raise site for this gate.
        ["RequiredSyntacticFeatureStruct"] = "MorphologicalRule.requiredPartsOfSpeech;PhonologicalSubrule.requiredPartsOfSpeech",
        // XmlLanguageLoader.cs:1159-1161.
        ["HeadRequiredSyntacticFeatureStruct"] = "CompoundingRule.headPartsOfSpeech",
        // XmlLanguageLoader.cs:1182-1184.
        ["NonHeadRequiredSyntacticFeatureStruct"] = "CompoundingRule.nonHeadPartsOfSpeech",
        // XmlLanguageLoader.cs:1217-1219.
        ["HeadProdRestrictMprFeatures"] = "CompoundingRule.headProdRestrictionsMprFeatures",
        // XmlLanguageLoader.cs:1220-1222.
        ["NonHeadProdRestrictMprFeatures"] = "CompoundingRule.nonHeadProdRestrictionsMprFeatures",
        // XmlLanguageLoader.cs:1057-1059 (MorphologicalInput, affix-process subrule), :1278 (Head-
        // MorphologicalInput, compounding subrule), :798-800 (PhonologicalSubrule, rewrite subrule).
        ["RequiredMprFeatures"] = "MorphologicalInput.requiredMPRFeatures;HeadMorphologicalInput.requiredMPRFeatures;PhonologicalSubrule.requiredMPRFeatures",
        // Same three elements, XmlLanguageLoader.cs:1060-1062/:1279/:801-803.
        ["ExcludedMprFeatures"] = "MorphologicalInput.excludedMPRFeatures;HeadMorphologicalInput.excludedMPRFeatures;PhonologicalSubrule.excludedMPRFeatures",
        // RootAllomorph.cs reads its OWN Allomorph.stemName (XmlLanguageLoader's Allomorph loader);
        // SynthesisAffixProcessRule.cs reads MorphologicalRule.requiredStemName (XmlLanguageLoader.cs:908-910).
        ["RequiredStemName"] = "Allomorph.stemName;MorphologicalRule.requiredStemName",
        // RootAllomorph.cs compares this allomorph's OWN Allomorph.stemName against a SIBLING
        // allomorph's Allomorph.stemName (same attribute, opposite direction of the check).
        ["ExcludedStemName"] = "Allomorph.stemName",
        // XmlLanguageLoader.cs:1333 (Slot.optional feeds Word.IsAllMorphologicalRulesApplied), :1304
        // (AffixTemplate.final feeds Word.IsLastAppliedRuleFinal, read by SynthesisStratumRule.cs's own
        // raise site). One raise site (Morpher.cs's RealizationalFeatureStruct check) additionally
        // depends on RealizationalRule's RealizationalFeatures ELEMENT, which carries no attribute.
        ["PartialParse"] = "Slot.optional;AffixTemplate.final",
        // XmlLanguageLoader.cs's Allomorph loader reads Allomorph.isBound directly onto RootAllomorph.IsBound.
        ["BoundRoot"] = "Allomorph.isBound",
        // AffixTemplate.final (XmlLanguageLoader.cs:1304) plus MorphologicalRule.partial
        // (XmlLanguageLoader.cs:865); CompoundingRule has no `partial` attribute of its own and reads
        // the word-level IsPartial state a MorphologicalRule raise earlier in the derivation set.
        ["NonPartialRuleProhibitedAfterFinalTemplate"] = "AffixTemplate.final;MorphologicalRule.partial",
        ["NonPartialRuleRequiredAfterNonFinalTemplate"] = "AffixTemplate.final;MorphologicalRule.partial",
        // XmlLanguageLoader.cs:867-869 (MorphologicalRule), :1154-1156 (CompoundingRule).
        ["MaxApplicationCount"] = "MorphologicalRule.multipleApplication;CompoundingRule.multipleApplication",
    };

    public static IReadOnlyList<Row> Compute(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        IReadOnlyDictionary<string, IReadOnlyList<string>> raiseSites = RaiseSiteScanner.Scan(repositoryRoot);
        IReadOnlyList<EngineGateWitnessSweep.Witness> witnesses = EngineGateWitnessSweep.Sweep(repositoryRoot);
        var witnessesByGate = witnesses.ToLookup(w => w.Gate, StringComparer.Ordinal);

        var rows = new List<Row>();
        foreach (FailureReason reason in Enum.GetValues<FailureReason>())
        {
            if (reason == FailureReason.None)
                continue;

            string gate = reason.ToString();
            string raiseSiteText = raiseSites.TryGetValue(gate, out IReadOnlyList<string>? sites)
                ? string.Join(';', sites)
                : NoAttributes;
            string dtdAttributeText = DtdAttributes.TryGetValue(gate, out string? attrs) ? attrs : NoAttributes;

            EngineGateWitnessSweep.Witness[] hits = witnessesByGate[gate].ToArray();
            string fixturesText = hits.Length == 0
                ? NoAttributes
                : string.Join(
                    ';',
                    hits.Select(h => h.FixtureId).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal)
                );
            string wordsText = hits.Length == 0
                ? NoAttributes
                : string.Join(
                    ';',
                    hits.Select(h => h.Word).Distinct(StringComparer.Ordinal).OrderBy(w => w, StringComparer.Ordinal)
                );
            EngineGateStatus status = hits.Length == 0 ? EngineGateStatus.Unreached : EngineGateStatus.Witnessed;

            rows.Add(new Row(gate, raiseSiteText, dtdAttributeText, fixturesText, wordsText, status));
        }

        return rows.OrderBy(r => r.Gate, StringComparer.Ordinal).ToArray();
    }

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
            "# GENERATED by hc-conformance --write-engine-gate-inventory. One row per SIL.Machine.Morphology."
        );
        writer.WriteLine(
            "# HermitCrab.FailureReason member (excluding None) -- HermitCrab's OWN enumeration of the"
        );
        writer.WriteLine(
            "# decisions it makes when declining to apply or unapply something, kept alongside (never"
        );
        writer.WriteLine(
            "# instead of) conformance/dataflow-obligations.tsv's DTD-attribute-pair denominator, because"
        );
        writer.WriteLine(
            "# the engine's own enum draws different lines than the schema does: it separates gates the DTD"
        );
        writer.WriteLine(
            "# fuses into one attribute and names several gates no DTD attribute pair expresses at all."
        );
        writer.WriteLine(
            "# raise_sites is mechanically scanned (RaiseSiteScanner); dtd_attributes is hand-verified"
        );
        writer.WriteLine(
            "# against XmlLanguageLoader.cs (see EngineGateInventoryLedger.DtdAttributes' own doc comment) and"
        );
        writer.WriteLine(
            "# '-' is a genuine finding for six gates, not a placeholder. triggered_by_fixtures/"
        );
        writer.WriteLine(
            "# triggering_words/status come from EngineGateWitnessSweep, an ACTUAL traced engine run over"
        );
        writer.WriteLine(
            "# every non-pathological, non-crash fixture -- status is Witnessed iff some word's trace tree"
        );
        writer.WriteLine(
            "# contains this FailureReason anywhere, a materially WEAKER claim than a dataflow-obligation"
        );
        writer.WriteLine(
            "# cell's same-word pair witness (see this file's own doc comment, and conformance/docs/"
        );
        writer.WriteLine("# how-it-is-computed.md, for why the two are not comparable strength).");
        writer.WriteLine("gate\traise_sites\tdtd_attributes\ttriggered_by_fixtures\ttriggering_words\tstatus");
        foreach (Row row in rows.OrderBy(r => r.Gate, StringComparer.Ordinal))
        {
            writer.WriteLine(
                string.Join(
                    '\t',
                    row.Gate,
                    row.RaiseSites,
                    row.DtdAttributes,
                    row.TriggeredByFixtures,
                    row.TriggeringWords,
                    row.Status
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
            if (!Enum.TryParse(fields[5], out EngineGateStatus status))
                throw new FormatException($"{RelativePath}: unknown status '{fields[5]}'");

            rows.Add(new Row(fields[0], fields[1], fields[2], fields[3], fields[4], status));
        }

        return rows;
    }
}
