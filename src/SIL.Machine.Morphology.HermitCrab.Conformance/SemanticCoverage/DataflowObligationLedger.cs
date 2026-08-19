#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Whether the corpus currently WITNESSES an obligation cell, as opposed to merely permitting it
/// structurally -- the presence/witness distinction <see cref="InterfaceWitnessLedger"/> already draws
/// for a single edge, applied here to a composed chain cell. <see cref="Unknown"/> is deliberate: this
/// generator marks a cell <see cref="Satisfied"/> only where a PAIR witness was found (see
/// <see cref="DataflowObligationLedger"/>'s own doc comment) and refuses every weaker predicate --
/// "writer witnessed somewhere, reader witnessed somewhere, possibly different words" is edge coverage
/// counted twice, which is exactly the fallacy this layer exists to catch.
/// </summary>
public enum ObligationStatus
{
    Satisfied,
    NotSatisfied,
    Unknown,
}

/// <summary>
/// A construct that can sit between a chain's write and its read and destroy the payload before the
/// reader sees it -- schema/engine-derived, never corpus-derived (a fixture happening to contain one
/// is a WITNESS fact, not a denominator fact). Each member names the exact engine mechanism, verified
/// against source:
/// <list type="bullet">
/// <item><see cref="Overwrite"/> -- <c>MprFeatureSet.AddOutput</c> (MprFeatureSet.cs): an
/// <c>outputType="overwrite"</c> <c>MorphologicalPhonologicalRuleFeatureGroup</c> drops every other
/// member of its group before unioning in a new write. Detected by <see cref="MutatingConstructs"/>,
/// reused unmodified from <see cref="InteractionChainLedger"/>. Applicable to every MPR-payload
/// chain (24).</item>
/// <item><see cref="Blocking"/> -- <c>Word.CheckBlocking</c> (Word.cs:472) into
/// <c>Word.SetRootAllomorph</c> (Word.cs:137): a blocked derivation is rebuilt as a brand-new
/// <see cref="Word"/> from a sibling lexical entry, which clears and reseeds <c>_mprFeatures</c>,
/// resets <c>SyntacticFeatureStruct</c>, and swaps <c>RootAllomorph</c> (hence <c>StemName</c>,
/// which reads live off it) -- killing all three payload types at once. Applicable to every chain
/// regardless of payload type (40): triggering it needs only ONE prior blockable rule application
/// (default <c>blockable="true"</c> on every <c>MorphologicalRule</c>/<c>RealizationalRule</c>/
/// <c>CompoundingRule</c>) between the write and the read, which a grammar can always arrange, and a
/// rule-output writer (<c>MorphologicalOutput.MPRFeatures</c>, <c>CompoundingRule.output*</c>,
/// <c>MorphologicalRule.outputPartOfSpeech</c>) gets that for free from its OWN post-application
/// blocking check. Realizing it in a GIVEN fixture additionally needs <c>LexEntry.Family</c> populated
/// (Word.cs:475-477) -- a real, checkable grammar-authoring fact, not a schema guarantee -- which is
/// exactly the corpus-level question <see cref="MutatorClassDetectors.HasEligibleFamily"/> answers.</item>
/// <item><see cref="PosPriorityUnion"/> -- <c>SynthesisAffixProcessRule.cs:181-182</c> and
/// <c>SynthesisCompoundingRule.cs:181-182</c>: <c>outWord.SyntacticFeatureStruct.PriorityUnion(
/// _rule.OutSyntacticFeatureStruct)</c> lets an intervening rule's own <c>outputPartOfSpeech</c>
/// overwrite a POS def placed earlier in the derivation, before a later <c>required*PartsOfSpeech</c>
/// read. Applicable only to <c>PartOfSpeech</c>-payload chains (15).</item>
/// <item><see cref="CompoundingNonHeadDrop"/> -- <c>SynthesisCompoundingRule.cs:236</c>:
/// <c>ApplySubrule</c> builds <c>output</c> from <c>headMatch.Input.Clone()</c> alone, so a non-head's
/// entire <c>MprFeatureSet</c> is never copied into <c>outWord.MprFeatures</c> at all -- no overwrite
/// group is even needed for the non-head's def to fail to reach a later reader. Applicable to any of
/// the three MPR writers, but only where the reader gates on the derivation's ACCUMULATED state at a
/// point a compounding step could sit before it (<c>MorphologicalInput</c>/<c>PhonologicalSubrule</c>'s
/// <c>required/excludedMPRFeatures</c>, 12 chains) -- see
/// <see cref="DataflowObligationLedger.IsCompoundingNonHeadDropReader"/> for the engine reasoning
/// excluding the other five MPR readers.</item>
/// </list>
/// </summary>
public enum ObligationMutatorClass
{
    Overwrite,
    Blocking,
    PosPriorityUnion,
    CompoundingNonHeadDrop,
}

/// <summary>
/// The checked-in denominator for the data-flow/MC/DC obligation-matrix layer
/// (docs/dataflow-coverage-plan.md), built on top of <see cref="InteractionChainLedger"/>'s chains.
///
/// <para>
/// <b>Every chain gates (no plain def-use class).</b> <see cref="SemanticInterfaceDirection"/>'s own
/// doc comment defines READ as "gates on derivation state something else placed there" -- that is
/// already true of every reader in every chain, not just attributes literally named
/// <c>required*</c>/<c>excluded*</c>. <c>CompoundingRule.headPartsOfSpeech</c> gates via
/// <c>HeadRequiredSyntacticFeatureStruct.Unify</c> (SynthesisCompoundingRule.cs:101) and
/// <c>headProdRestrictionsMprFeatures</c> gates via <c>MprFeatureSet.CompoundMprFeaturesMatch</c>
/// (SynthesisCompoundingRule.cs:116) exactly as the *-prefixed attributes do. So every chain gets the
/// base MC/DC four cells: <c>PresentControl</c>/<c>PresentGatedForm</c>/<c>AbsentControl</c>/
/// <c>AbsentGatedForm</c> -- 40 chains x 4 = 160 is the schema floor.
/// </para>
///
/// <para>
/// <b>MC/DC is per condition, not per gate.</b> <c>MprFeatureSet.IsMatchRequired</c>/
/// <c>IsMatchExcluded</c> (MprFeatureSet.cs:46,72) are a conjunction across ungrouped features and
/// <c>matchType="all"</c> groups, a disjunction across <c>matchType="any"</c> groups -- an n-condition
/// boolean whenever a <c>required*</c>/<c>excludedMPRFeatures</c> value lists n>1 feature ids (real
/// case: <c>mpr-overwrite-order-dependence/grammar.xml:145</c>, <c>requiredMPRFeatures="mprP mprQ"</c>).
/// MC/DC over n conditions needs n+1 cases, not a fixed 2. This generator scans every MPR-payload
/// chain's exercising fixtures for the largest observed token count and, where n>1, emits
/// <c>ConditionExtension</c> rows on top of the floor's four, attributed to the specific fixture that
/// realizes it (see <see cref="ScanLargestConditionCount"/>). <c>CompoundMprFeaturesMatch</c>
/// (MprFeatureSet.cs:98, <c>Count == 0 || Intersect.Any()</c>) is a DIFFERENT decision shape --
/// vacuously true on an empty restriction set, otherwise a disjunction -- so it is scanned with the
/// same token-count mechanism but never assimilated to the conjunction/disjunction-per-group reading.
/// POS and StemName reader gates (FeatureStruct unification, not a flat id-set) are not decomposed
/// this way yet -- floor only, named as a scoping boundary rather than silently assumed complete.
/// </para>
///
/// <para>
/// <b>Cell satisfaction requires a PAIR witness on the SAME word.</b> "Writer witnessed somewhere,
/// reader witnessed somewhere" is edge coverage counted twice; a chain cell is satisfied only when
/// severing the writer flips a named word's outcome AND severing the reader flips THAT SAME word's
/// outcome. This generator gets both severance runs for free by reading the already-computed, already
/// checked-in <see cref="InterfaceWitnessLedger"/> (conformance/interface-witness.tsv) rather than
/// re-running any engine sweep: for each exercising fixture, it joins the writer's and reader's rows on
/// (fixture, ExampleWord) and requires both <see cref="CounterfactualVerdict.Evidenced"/> (a
/// <see cref="CounterfactualVerdict.Timeout"/> row is never treated as witness, per the same rule
/// <see cref="InterfaceWitnessLedger"/> itself follows -- "I could not look" must never read as "fine").
/// When the flip is a clean fail-to-pass on both sides (baseline blocked, severing either side
/// unblocks it -- the textbook shape coverage-strategy.md's gate section describes) and the reader
/// attribute names its own polarity (<c>required*</c> => blocks on absence, <c>excludedMPRFeatures</c>-
/// family => blocks on presence), the specific MC/DC arm that word demonstrates is marked
/// <see cref="ObligationStatus.Satisfied"/>; every other pattern (no paired witness, a
/// non-fail/pass flip, or a reader name this generator cannot read a polarity from, e.g.
/// <c>head</c>/<c>nonHeadPartsOfSpeech</c>) is left <see cref="ObligationStatus.Unknown"/> rather than
/// guessed.
/// </para>
///
/// <para>
/// <b>The kill-path denominator is chains x mutator classes, schema-derived.</b> A chain's applicable
/// <see cref="ObligationMutatorClass"/> set is fixed by its payload type, writer and (for
/// <see cref="ObligationMutatorClass.CompoundingNonHeadDrop"/>) reader, never by which fixture happens
/// to exist (see <see cref="ApplicableMutatorClasses"/>): today that is 24 MPR chains x
/// <see cref="ObligationMutatorClass.Overwrite"/>, 40 chains (every chain) x
/// <see cref="ObligationMutatorClass.Blocking"/>, 15 PartOfSpeech chains x
/// <see cref="ObligationMutatorClass.PosPriorityUnion"/>, and 12 chains (all three MPR writers, but only
/// the <c>MorphologicalInput</c>/<c>PhonologicalSubrule</c> readers -- see
/// <see cref="IsCompoundingNonHeadDropReader"/> for why the other five MPR readers are engine-proven
/// unreachable by this mutator) x <see cref="ObligationMutatorClass.CompoundingNonHeadDrop"/> -- 91
/// (chain, class) candidates, each contributing a <c>MutatorAbsent</c>/<c>MutatorPresent</c> pair. Every
/// class now has a real structural detector (<see cref="MutatorClassDetectors"/> for the three added
/// here; <see cref="MutatingConstructs"/>, reused unmodified from <see cref="InteractionChainLedger"/>,
/// for <see cref="ObligationMutatorClass.Overwrite"/>) that checks the exercising fixture(s) for the
/// engine's own necessary precondition -- never a proof of temporal ordering or of a specific word's
/// outcome, so status stays <see cref="ObligationStatus.Unknown"/> either way; only
/// <see cref="FindPairedWitness"/>'s same-word severance pair ever produces
/// <see cref="ObligationStatus.Satisfied"/>.
/// </para>
/// </summary>
public static class DataflowObligationLedger
{
    public const string RelativePath = "conformance/dataflow-obligations.tsv";

    private const int ColumnCount = 11;

    public sealed record Row(
        string CellId,
        string WriterElement,
        string WriterAttribute,
        string PayloadType,
        string ReaderElement,
        string ReaderAttribute,
        string CellKind, // "McDc" | "ConditionExtension" | "Mutator"
        string Role,
        string MutatorClass, // "-" unless CellKind == "Mutator"
        ObligationStatus Status,
        string Evidence
    );

    private const string NoMutatorClass = "-";

    // ----------------------------------------------------------------------------------------
    // Mutator-class applicability (schema/engine-derived, not corpus-derived).
    // ----------------------------------------------------------------------------------------

    public static IReadOnlyList<ObligationMutatorClass> ApplicableMutatorClasses(InteractionChainLedger.Row chain)
    {
        ArgumentNullException.ThrowIfNull(chain);
        var classes = new List<ObligationMutatorClass> { ObligationMutatorClass.Blocking };

        if (chain.PayloadType == "MorphologicalPhonologicalRuleFeature")
        {
            classes.Add(ObligationMutatorClass.Overwrite);
            if (IsCompoundingNonHeadDropReader(chain.ReaderElement, chain.ReaderAttribute))
                classes.Add(ObligationMutatorClass.CompoundingNonHeadDrop);
        }

        if (chain.PayloadType == "PartOfSpeech")
            classes.Add(ObligationMutatorClass.PosPriorityUnion);

        return classes;
    }

    /// <summary>
    /// <see cref="ObligationMutatorClass.CompoundingNonHeadDrop"/> is NOT applicable to every
    /// MPR-payload chain a naive "writer is LexicalEntry.ruleFeatures" rule would suggest -- engine
    /// verification narrows AND widens that guess, and both corrections are load-bearing (the
    /// inclusion rule in docs/coverage-strategy.md: a mutator that structurally cannot change a
    /// parse for a given chain must not generate an obligation for it):
    /// <list type="bullet">
    /// <item>NARROWED: <c>CompoundingRule.headProdRestrictionsMprFeatures</c> and
    /// <c>HeadMorphologicalInput.{required,excluded}MPRFeatures</c> all gate on
    /// <c>input.MprFeatures</c> -- the HEAD side, read BEFORE this same rule's own drop step
    /// (SynthesisCompoundingRule.cs:116,136-137,153-154). For a writer's id to reach one of these
    /// readers at all, it must have arrived via the undropped head path, so no drop event ever sits
    /// between that write and that read. <c>CompoundingRule.nonHeadProdRestrictionsMprFeatures</c> is
    /// read only by <c>AnalysisCompoundingRule</c> (line 76-77), straight off the candidate
    /// <c>LexEntry</c>'s own <c>MprFeatures</c> during unapplication -- a lexicon read, not the
    /// derivation-accumulated state this generator's chains model, and outside
    /// docs/dataflow-coverage-plan.md's own named "analysis direction" exception besides.</item>
    /// <item>WIDENED: the drop empties the ENTIRE <c>Word.MprFeatures</c> bag regardless of which
    /// construct wrote into it, so it reaches <c>MorphologicalInput</c>/<c>PhonologicalSubrule</c>
    /// readers from ALL THREE MPR writers (<c>LexicalEntry.ruleFeatures</c>,
    /// <c>MorphologicalOutput.MPRFeatures</c>, <c>CompoundingRule.outputProdRestrictionsMprFeatures</c>),
    /// not only the lexical-entry one.</item>
    /// </list>
    /// Net effect: 12 chains (3 writers x these 4 readers), not the 8 a writer-only rule would produce.
    /// </summary>
    private static bool IsCompoundingNonHeadDropReader(string readerElement, string readerAttribute) =>
        (readerElement == "MorphologicalInput" || readerElement == "PhonologicalSubrule")
        && (readerAttribute == "requiredMPRFeatures" || readerAttribute == "excludedMPRFeatures");

    // ----------------------------------------------------------------------------------------
    // Condition-count scan for the MC/DC extension (point 2): n = the largest token count this
    // generator finds, across the chain's own exercising fixtures, on a reader-attribute occurrence
    // that actually carries one of the writer/reader shared ids. Mirrors the tokenizing
    // InteractionChainLedger/MutatingConstructs already do, without modifying either file.
    // ----------------------------------------------------------------------------------------

    private sealed record ConditionScanResult(int MaxTokenCount, string FixtureId, string ObservedValue);

    private static IReadOnlyList<string> Tokenize(string? value) =>
        string.IsNullOrEmpty(value)
            ? Array.Empty<string>()
            : value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Only meaningful for an MPR-feature payload: <c>IsMatchRequired</c>/<c>IsMatchExcluded</c>/
    /// <c>CompoundMprFeaturesMatch</c> are all n-ary over a flat, whitespace-separated IDREFS value.
    /// POS/StemName reader gates are FeatureStruct unification, not decomposed here (see this class's
    /// own doc comment) -- deliberately out of scope, not silently assumed single-condition.
    /// </summary>
    private static ConditionScanResult? ScanLargestConditionCount(
        string repositoryRoot,
        InteractionChainLedger.Row chain
    )
    {
        if (chain.PayloadType != "MorphologicalPhonologicalRuleFeature")
            return null;

        int bestCount = 1;
        string? bestFixture = null;
        string? bestValue = null;

        foreach (string fixtureId in chain.ExercisingFixtures)
        {
            string grammarPath = Path.Combine(
                repositoryRoot,
                "conformance",
                fixtureId.Replace('/', Path.DirectorySeparatorChar),
                "grammar.xml"
            );
            if (!File.Exists(grammarPath))
                continue;

            XDocument grammar = XDocument.Load(grammarPath);

            var writerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (XElement owner in grammar.Descendants(chain.WriterElement))
            {
                foreach (string token in Tokenize((string?)owner.Attribute(chain.WriterAttribute)))
                    writerIds.Add(token);
            }

            foreach (XElement owner in grammar.Descendants(chain.ReaderElement))
            {
                string? rawValue = (string?)owner.Attribute(chain.ReaderAttribute);
                IReadOnlyList<string> readerTokens = Tokenize(rawValue);
                if (readerTokens.Count == 0 || !readerTokens.Any(writerIds.Contains))
                    continue;

                if (readerTokens.Count > bestCount)
                {
                    bestCount = readerTokens.Count;
                    bestFixture = fixtureId;
                    bestValue = rawValue;
                }
            }
        }

        return bestFixture is null ? null : new ConditionScanResult(bestCount, bestFixture, bestValue!);
    }

    // ----------------------------------------------------------------------------------------
    // Pair-witness satisfaction (point 3): join the writer's and reader's already-computed
    // InterfaceWitnessLedger rows for the SAME fixture on the SAME example word.
    // ----------------------------------------------------------------------------------------

    private static bool IsFailOutcome(string? outcome) =>
        outcome != null && outcome.EndsWith("::-", StringComparison.Ordinal);

    private readonly record struct PairedWitness(string FixtureId, string Word, string Role);

    /// <summary>
    /// Finds, at most, one paired witness for a chain: the first exercising fixture (in the chain's own
    /// sorted order) where the writer's and reader's severance rows both flip the SAME word from a
    /// failed parse to a successful one -- the shape that means the gate was actively blocking that
    /// word, and severing either side of the chain unblocks it. Direction is read off the reader
    /// attribute's own name (<c>required*</c> => blocks on absence => <c>AbsentGatedForm</c>;
    /// <c>excluded*</c> => blocks on presence => <c>PresentGatedForm</c>); every other reader name
    /// (e.g. <c>headPartsOfSpeech</c>) or flip shape returns null -- a real paired witness may still
    /// exist there, this generator just does not attempt to classify it.
    /// </summary>
    private static PairedWitness? FindPairedWitness(
        InteractionChainLedger.Row chain,
        IReadOnlyDictionary<(string Element, string Attribute, string Fixture), InterfaceWitnessResult> witnessByKey
    )
    {
        string role;
        if (chain.ReaderAttribute.StartsWith("required", StringComparison.Ordinal))
            role = "AbsentGatedForm";
        else if (chain.ReaderAttribute.StartsWith("excluded", StringComparison.Ordinal))
            role = "PresentGatedForm";
        else
            return null;

        foreach (string fixtureId in chain.ExercisingFixtures)
        {
            if (
                !witnessByKey.TryGetValue(
                    (chain.WriterElement, chain.WriterAttribute, fixtureId),
                    out InterfaceWitnessResult? w
                )
                || !witnessByKey.TryGetValue(
                    (chain.ReaderElement, chain.ReaderAttribute, fixtureId),
                    out InterfaceWitnessResult? r
                )
            )
            {
                continue;
            }

            if (w.Verdict != CounterfactualVerdict.Evidenced || r.Verdict != CounterfactualVerdict.Evidenced)
                continue;
            if (w.ExampleWord == null || w.ExampleWord != r.ExampleWord)
                continue;

            bool writerFlipsFailToPass = IsFailOutcome(w.ExampleOutcome) && !IsFailOutcome(w.CounterexampleOutcome);
            bool readerFlipsFailToPass = IsFailOutcome(r.ExampleOutcome) && !IsFailOutcome(r.CounterexampleOutcome);
            if (writerFlipsFailToPass && readerFlipsFailToPass)
                return new PairedWitness(fixtureId, w.ExampleWord, role);
        }

        return null;
    }

    // ----------------------------------------------------------------------------------------
    // Compute
    // ----------------------------------------------------------------------------------------

    public static IReadOnlyList<Row> Compute(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        IReadOnlyList<InteractionChainLedger.Row> chains = InteractionChainLedger.Compute(repositoryRoot);
        IReadOnlyList<InterfaceWitnessResult> witnessRows = InterfaceWitnessLedger.Read(repositoryRoot);
        var witnessByKey = witnessRows.ToDictionary(r => (r.Element, r.Attribute, r.FixtureId));

        var rows = new List<Row>();
        foreach (InteractionChainLedger.Row chain in chains)
        {
            PairedWitness? paired = chain.Exercised ? FindPairedWitness(chain, witnessByKey) : null;

            foreach (string role in new[] { "PresentControl", "PresentGatedForm", "AbsentControl", "AbsentGatedForm" })
            {
                (ObligationStatus status, string evidence) = EvaluateMcDcCell(chain, role, paired);
                rows.Add(BuildRow(chain, "McDc", role, NoMutatorClass, status, evidence, extensionKey: null));
            }

            ConditionScanResult? conditionScan = ScanLargestConditionCount(repositoryRoot, chain);
            if (conditionScan is { MaxTokenCount: > 1 } scan)
            {
                // MC/DC over n conditions needs n+1 cases; the floor already carries 2 (present/absent),
                // so n-1 extra McDc-style vectors are required, each still crossed with the
                // control/gated-form axis that attributes the outcome (coverage-strategy.md's "every
                // claim needs a control").
                for (int vector = 3; vector <= scan.MaxTokenCount + 1; vector++)
                {
                    foreach (string half in new[] { "Control", "GatedForm" })
                    {
                        string role = $"McDcVector{vector}{half}";
                        string evidence =
                            $"mechanically required by {scan.MaxTokenCount}-condition gate observed in "
                            + $"{scan.FixtureId} ('{scan.ObservedValue}'); no per-vector witness check implemented yet";
                        rows.Add(
                            BuildRow(
                                chain,
                                "ConditionExtension",
                                role,
                                NoMutatorClass,
                                ObligationStatus.Unknown,
                                evidence,
                                extensionKey: scan.FixtureId
                            )
                        );
                    }
                }
            }

            foreach (ObligationMutatorClass mutatorClass in ApplicableMutatorClasses(chain))
            {
                (ObligationStatus status, string evidence) = EvaluateMutatorClass(repositoryRoot, chain, mutatorClass);
                foreach (string role in new[] { "MutatorAbsent", "MutatorPresent" })
                {
                    rows.Add(
                        BuildRow(chain, "Mutator", role, mutatorClass.ToString(), status, evidence, extensionKey: null)
                    );
                }
            }
        }

        return Sort(rows);
    }

    private static (ObligationStatus, string) EvaluateMcDcCell(
        InteractionChainLedger.Row chain,
        string role,
        PairedWitness? paired
    )
    {
        if (!chain.Exercised)
        {
            return (
                ObligationStatus.NotSatisfied,
                "chain unexercised: no fixture sets writer and reader to a shared id (conformance/interaction-chains.tsv)"
            );
        }

        if (paired is { } p && p.Role == role)
        {
            return (
                ObligationStatus.Satisfied,
                $"paired witness: severing writer and reader both flip '{p.Word}' from failed to successful parse in {p.FixtureId} (conformance/interface-witness.tsv)"
            );
        }

        return (
            ObligationStatus.Unknown,
            "chain exercised (conformance/interaction-chains.tsv) but no same-word paired severance witness (writer+reader both Evidenced on the same example word in conformance/interface-witness.tsv) was found for this arm"
        );
    }

    private static (ObligationStatus, string) EvaluateMutatorClass(
        string repositoryRoot,
        InteractionChainLedger.Row chain,
        ObligationMutatorClass mutatorClass
    )
    {
        if (!chain.Exercised)
        {
            return (
                ObligationStatus.NotSatisfied,
                "chain unexercised: no fixture sets writer and reader to a shared id (conformance/interaction-chains.tsv)"
            );
        }

        switch (mutatorClass)
        {
            case ObligationMutatorClass.Overwrite:
                return chain.Hazardous
                    ? (
                        ObligationStatus.Unknown,
                        "structurally hazardous (conformance/interaction-chains.tsv: an exercising fixture has an overwrite-type MorphologicalPhonologicalRuleFeatureGroup covering the shared id) but word-level witness of the kill itself is not independently checked here"
                    )
                    : (
                        ObligationStatus.Unknown,
                        "no exercising fixture has an overwrite-type MorphologicalPhonologicalRuleFeatureGroup covering the shared id yet"
                    );

            case ObligationMutatorClass.Blocking:
                return EvaluateBlocking(repositoryRoot, chain);

            case ObligationMutatorClass.PosPriorityUnion:
                return EvaluatePosPriorityUnion(repositoryRoot, chain);

            case ObligationMutatorClass.CompoundingNonHeadDrop:
                return EvaluateCompoundingNonHeadDrop(repositoryRoot, chain);

            default:
                throw new ArgumentOutOfRangeException(nameof(mutatorClass), mutatorClass, "unhandled mutator class");
        }
    }

    private static (ObligationStatus, string) EvaluateBlocking(string repositoryRoot, InteractionChainLedger.Row chain)
    {
        (bool eligible, string? fixtureId) = MutatorClassDetectors.ScanForAny(
            repositoryRoot,
            chain.ExercisingFixtures,
            MutatorClassDetectors.HasEligibleFamily
        );

        return eligible
            ? (
                ObligationStatus.Unknown,
                $"structurally hazardous: {fixtureId} declares >=2 LexicalEntry sharing a family under the same Stratum (Word.CheckBlocking's family+stratum precondition, Word.cs:475-485); FeatureStruct subsumption and word-level witness of an actual block are not checked here"
            )
            : (
                ObligationStatus.Unknown,
                "no exercising fixture declares >=2 LexicalEntry sharing a family under the same Stratum -- Word.CheckBlocking (Word.cs:475-477) requires LexEntry.Family != null, so blocking cannot fire in any exercising fixture as authored"
            );
    }

    private static (ObligationStatus, string) EvaluatePosPriorityUnion(
        string repositoryRoot,
        InteractionChainLedger.Row chain
    )
    {
        (int maxCount, string? fixtureId) = MutatorClassDetectors.ScanForMax(
            repositoryRoot,
            chain.ExercisingFixtures,
            MutatorClassDetectors.CountPosWritingRuleElements
        );

        // A rule-element writer (MorphologicalRule/CompoundingRule.outputPartOfSpeech) counts as its own
        // occurrence, so a genuinely different intervening mutator needs a second one; a LexicalEntry.
        // partOfSpeech writer is not a rule at all, so any occurrence at all is a candidate mutator.
        bool writerIsRuleElement = chain.WriterElement is "MorphologicalRule" or "CompoundingRule";
        int threshold = writerIsRuleElement ? 2 : 1;

        return maxCount >= threshold
            ? (
                ObligationStatus.Unknown,
                $"structurally hazardous: {fixtureId} declares {maxCount} outputPartOfSpeech-bearing MorphologicalRule/CompoundingRule element(s) -- SynthesisAffixProcessRule.cs:181-182/SynthesisCompoundingRule.cs:181-182's unconditional PriorityUnion gives each one a chance to clobber POS before this reader's gate; which element actually intervenes, and word-level witness of an actual clobber, are not checked here"
            )
            : (
                ObligationStatus.Unknown,
                "no exercising fixture declares a second outputPartOfSpeech-bearing rule besides (or, for a LexicalEntry writer, at all beyond) the chain's own writer -- PriorityUnion has nothing else to clobber with in this fixture as authored"
            );
    }

    private static (ObligationStatus, string) EvaluateCompoundingNonHeadDrop(
        string repositoryRoot,
        InteractionChainLedger.Row chain
    )
    {
        (bool hasRule, string? fixtureId) = MutatorClassDetectors.ScanForAny(
            repositoryRoot,
            chain.ExercisingFixtures,
            MutatorClassDetectors.HasCompoundingRule
        );

        return hasRule
            ? (
                ObligationStatus.Unknown,
                $"structurally hazardous: {fixtureId} declares a CompoundingRule -- SynthesisCompoundingRule.ApplySubrule (SynthesisCompoundingRule.cs:236) builds output from the head alone, so the non-head's entire MprFeatureSet is dropped unconditionally whenever this rule applies with the writer's word as non-head; which word actually plays non-head, and word-level witness of the drop, are not checked here"
            )
            : (
                ObligationStatus.Unknown,
                "no exercising fixture declares a CompoundingRule -- the drop mechanism (SynthesisCompoundingRule.cs:236) cannot fire in any exercising fixture as authored"
            );
    }

    private static Row BuildRow(
        InteractionChainLedger.Row chain,
        string cellKind,
        string role,
        string mutatorClass,
        ObligationStatus status,
        string evidence,
        string? extensionKey
    )
    {
        string cellId =
            $"{chain.PayloadType}::{chain.WriterElement}.{chain.WriterAttribute}->{chain.ReaderElement}.{chain.ReaderAttribute}::{cellKind}:{role}"
            + (mutatorClass == NoMutatorClass ? "" : $":{mutatorClass}")
            + (extensionKey == null ? "" : $":{extensionKey}");

        return new Row(
            cellId,
            chain.WriterElement,
            chain.WriterAttribute,
            chain.PayloadType,
            chain.ReaderElement,
            chain.ReaderAttribute,
            cellKind,
            role,
            mutatorClass,
            status,
            evidence
        );
    }

    private static IReadOnlyList<Row> Sort(IEnumerable<Row> rows) =>
        rows.OrderBy(r => r.PayloadType, StringComparer.Ordinal)
            .ThenBy(r => r.WriterElement, StringComparer.Ordinal)
            .ThenBy(r => r.WriterAttribute, StringComparer.Ordinal)
            .ThenBy(r => r.ReaderElement, StringComparer.Ordinal)
            .ThenBy(r => r.ReaderAttribute, StringComparer.Ordinal)
            .ThenBy(r => r.CellKind, StringComparer.Ordinal)
            .ThenBy(r => r.MutatorClass, StringComparer.Ordinal)
            .ThenBy(r => r.Role, StringComparer.Ordinal)
            .ThenBy(r => r.CellId, StringComparer.Ordinal)
            .ToArray();

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
        writer.WriteLine("# GENERATED by hc-conformance --write-dataflow-obligations. One row per required CELL of");
        writer.WriteLine(
            "# docs/dataflow-coverage-plan.md's obligation matrix, built on conformance/interaction-chains.tsv"
        );
        writer.WriteLine(
            "# (InteractionChainLedger). Every chain gates (see DataflowObligationLedger's own doc comment for"
        );
        writer.WriteLine(
            "# why the old required*/excluded*-name heuristic was wrong), so cell_kind=McDc contributes the"
        );
        writer.WriteLine("# floor 4 cells per chain (PresentControl/PresentGatedForm/AbsentControl/AbsentGatedForm).");
        writer.WriteLine(
            "# cell_kind=ConditionExtension adds the extra MC/DC vectors an MPR-feature gate needs when a real"
        );
        writer.WriteLine(
            "# fixture's attribute value lists more than one feature id (n conditions need n+1 cases, not a"
        );
        writer.WriteLine("# fixed 2) -- attributed to the specific fixture that realizes it. cell_kind=Mutator adds a");
        writer.WriteLine(
            "# MutatorAbsent/MutatorPresent pair per (chain, ObligationMutatorClass) the payload type/writer"
        );
        writer.WriteLine(
            "# make schema-applicable -- mutator_class names which one; only Overwrite has a corpus detector"
        );
        writer.WriteLine(
            "# today, the other three are declared-but-unmodelled. status is Satisfied only for a same-word"
        );
        writer.WriteLine("# PAIR witness (writer AND reader severance both flip one named word -- see");
        writer.WriteLine("# conformance/interface-witness.tsv); NotSatisfied when the chain is not even structurally");
        writer.WriteLine("# exercised; Unknown otherwise -- deliberate, not a gap (see this class's own doc comment).");
        writer.WriteLine(
            "cell_id\twriter_element\twriter_attribute\tpayload_type\treader_element\treader_attribute\tcell_kind\trole\tmutator_class\tstatus\tevidence"
        );
        foreach (Row row in Sort(rows))
        {
            writer.WriteLine(
                string.Join(
                    '\t',
                    row.CellId,
                    row.WriterElement,
                    row.WriterAttribute,
                    row.PayloadType,
                    row.ReaderElement,
                    row.ReaderAttribute,
                    row.CellKind,
                    row.Role,
                    row.MutatorClass,
                    row.Status,
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
                || line.StartsWith("cell_id\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields.Length != ColumnCount)
                throw new FormatException($"{RelativePath}: '{line}' must be {ColumnCount} tab-separated fields");
            if (!Enum.TryParse(fields[9], out ObligationStatus status))
                throw new FormatException($"{RelativePath}: unknown status '{fields[9]}'");

            rows.Add(
                new Row(
                    fields[0],
                    fields[1],
                    fields[2],
                    fields[3],
                    fields[4],
                    fields[5],
                    fields[6],
                    fields[7],
                    fields[8],
                    status,
                    fields[10]
                )
            );
        }

        return rows;
    }
}
