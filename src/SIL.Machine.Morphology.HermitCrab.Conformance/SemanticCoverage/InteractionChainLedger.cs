#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// The write/read/ref direction of every declared IDREF/IDREFS interface, as a SEMANTIC judgment
/// checked against what the engine actually does with each value -- deliberately not
/// <see cref="InterfaceInventoryLedger"/>'s <see cref="InterfaceDirectionClassifier"/>, which infers
/// direction from an attribute-name prefix. That heuristic is demonstrably wrong: a real field grammar
/// (a Bantu language) writes zero <c>MorphologicalOutput.MPRFeatures</c> and instead carries all of its
/// inflection-class/productivity-restriction features through <c>LexicalEntry.ruleFeatures</c> --
/// which the prefix rule calls "ref" (it starts with neither <c>output</c> nor <c>assigned</c>) even
/// though <c>XmlLanguageLoader.TryLoadLexEntry</c> unions it straight into <c>entry.MprFeatures</c>, the
/// exact accumulator <c>required</c>/<c>excludedMPRFeatures</c> later gate on. A prefix is a
/// correlation the DTD's authors mostly followed, not a rule the engine enforces, so every one of the
/// 60 declared interfaces below was re-decided on what it does: WRITE places a value into derivation
/// state that something else later reads; READ gates on derivation state something else placed there;
/// REF is a structural pointer to a fixed definition, participating in no write/read pair. Two more
/// reclassifications fell out of applying that same test rather than patching the one reported case:
/// <c>LexicalEntry.partOfSpeech</c> seeds <c>entry.SyntacticFeatureStruct</c> exactly as
/// <c>ruleFeatures</c> seeds <c>entry.MprFeatures</c> (read by every <c>requiredPartsOfSpeech</c>-family
/// gate), and <c>Allomorph.stemName</c> labels <c>allomorph.StemName</c>, read by
/// <c>MorphologicalRule.requiredStemName</c> (<c>SynthesisAffixProcessRule</c>) -- a junction
/// (<c>StemName</c>) the prefix heuristic could not see at all, since <c>InterfaceInventoryLedger</c>
/// only asks the writer/reader question of the two payload types it already found writers and readers
/// for one at a time by name.
/// </summary>
internal static class SemanticInterfaceDirection
{
    // Grouped by element for review. A short rationale sits next to every reclassified or otherwise
    // non-obvious entry; an unremarked entry means the prefix-implied direction and the semantic one
    // agree. "dead" means the DTD declares it but no engine source references the attribute at all
    // (grep-confirmed against src/SIL.Machine.Morphology.HermitCrab); syntactic subcategorization and
    // the two LexicalEntry obligatory*Features attributes are exceptions, direction is recorded for
    // completeness but neither can ever form a junction.
    private static readonly IReadOnlyDictionary<(string Element, string Attribute), InterfaceDirection> Table =
        new Dictionary<(string, string), InterfaceDirection>
        {
            [("AffixTemplate", "requiredPartsOfSpeech")] = InterfaceDirection.Read,
            [("AffixTemplate", "requiredSubcategorizedRules")] = InterfaceDirection.Read, // dead (subcategorization)

            // Constrains against which OTHER allomorphs/morphemes the derivation already selected --
            // structural membership, not a value any declared attribute writes (MorphemeCoOccurrenceRule /
            // AllomorphCoOccurrenceRule match on Word/Morpheme identity directly).
            [("Allomorph", "stemName")] = InterfaceDirection.Write, // RECLASSIFIED: seeds allomorph.StemName (XmlLanguageLoader), read by MorphologicalRule.requiredStemName
            [("AllomorphCoOccurrenceRule", "otherAllomorphs")] = InterfaceDirection.Ref,
            [("AllomorphCoOccurrenceRule", "primaryAllomorph")] = InterfaceDirection.Ref,
            [("AlphaVariable", "variableFeature")] = InterfaceDirection.Ref,
            [("BoundaryMarker", "boundary")] = InterfaceDirection.Ref,

            [("CompoundingRule", "headPartsOfSpeech")] = InterfaceDirection.Read,
            [("CompoundingRule", "headProdRestrictionsMprFeatures")] = InterfaceDirection.Read,
            [("CompoundingRule", "headSubcategorizedRules")] = InterfaceDirection.Read, // dead (subcategorization)
            [("CompoundingRule", "nonHeadPartsOfSpeech")] = InterfaceDirection.Read,
            [("CompoundingRule", "nonHeadProdRestrictionsMprFeatures")] = InterfaceDirection.Read,
            [("CompoundingRule", "nonHeadSubcategorizedRules")] = InterfaceDirection.Read, // dead (subcategorization)
            [("CompoundingRule", "outputObligatoryFeatures")] = InterfaceDirection.Write, // writes Word.ObligatorySyntacticFeatures; only read by Morpher's own final-word validation, not by another declared attribute -- no junction forms
            [("CompoundingRule", "outputPartOfSpeech")] = InterfaceDirection.Write,
            [("CompoundingRule", "outputProdRestrictionsMprFeatures")] = InterfaceDirection.Write,
            [("CompoundingRule", "outputSubcategorization")] = InterfaceDirection.Write, // dead (subcategorization)

            [("CopyFromInput", "index")] = InterfaceDirection.Ref,
            [("FeatureValue", "feature")] = InterfaceDirection.Ref,
            [("FeatureValue", "symbolValues")] = InterfaceDirection.Ref,

            [("HeadMorphologicalInput", "excludedMPRFeatures")] = InterfaceDirection.Read,
            [("HeadMorphologicalInput", "requiredMPRFeatures")] = InterfaceDirection.Read,

            [("InsertSegments", "characterDefinitionTable")] = InterfaceDirection.Ref,

            // LexicalEntry.family blocks other members of the SAME family from also matching -- checked
            // directly against family membership, not against a value any other declared attribute reads.
            [("LexicalEntry", "family")] = InterfaceDirection.Ref,
            [("LexicalEntry", "morphologicalRules")] = InterfaceDirection.Ref, // dead: no loader reference at all
            [("LexicalEntry", "obligatoryFootFeatures")] = InterfaceDirection.Read, // dead: no loader reference at all
            [("LexicalEntry", "obligatoryHeadFeatures")] = InterfaceDirection.Read, // dead: no loader reference at all
            [("LexicalEntry", "partOfSpeech")] = InterfaceDirection.Write, // RECLASSIFIED: seeds entry.SyntacticFeatureStruct (XmlLanguageLoader.TryLoadLexEntry), read by every requiredPartsOfSpeech-family gate
            [("LexicalEntry", "ruleFeatures")] = InterfaceDirection.Write, // RECLASSIFIED: seeds entry.MprFeatures (XmlLanguageLoader.TryLoadLexEntry), read by every required/excludedMPRFeatures-family gate
            [("LexicalEntry", "subcategorizations")] = InterfaceDirection.Ref, // dead (subcategorization)

            [("MetathesisRule", "leftSwitch")] = InterfaceDirection.Ref,
            [("MetathesisRule", "rightSwitch")] = InterfaceDirection.Ref,
            [("ModifyFromInput", "index")] = InterfaceDirection.Ref,
            [("MorphemeCoOccurrenceRule", "otherMorphemes")] = InterfaceDirection.Ref,
            [("MorphemeCoOccurrenceRule", "primaryMorpheme")] = InterfaceDirection.Ref,

            [("MorphologicalInput", "excludedMPRFeatures")] = InterfaceDirection.Read,
            [("MorphologicalInput", "requiredMPRFeatures")] = InterfaceDirection.Read,
            [("MorphologicalOutput", "MPRFeatures")] = InterfaceDirection.Write,

            // The mutator's own group membership -- consumed by MprFeatureSet.AddOutput as configuration,
            // not itself a write or read of the derivation payload.
            [("MorphologicalPhonologicalRuleFeatureGroup", "features")] = InterfaceDirection.Ref,

            [("MorphologicalRule", "outputObligatoryFeatures")] = InterfaceDirection.Write, // see CompoundingRule.outputObligatoryFeatures: no attribute reads it, no junction
            [("MorphologicalRule", "outputPartOfSpeech")] = InterfaceDirection.Write,
            [("MorphologicalRule", "outputSubcategorization")] = InterfaceDirection.Write, // dead (subcategorization)
            [("MorphologicalRule", "requiredPartsOfSpeech")] = InterfaceDirection.Read,
            [("MorphologicalRule", "requiredStemName")] = InterfaceDirection.Read, // reads input.RootAllomorph.StemName (SynthesisAffixProcessRule)
            [("MorphologicalRule", "requiredSubcategorizedRules")] = InterfaceDirection.Read, // dead (subcategorization)

            [("OutputSubcategorizationOverride", "inputSubcategorization")] = InterfaceDirection.Ref, // dead (subcategorization)
            [("OutputSubcategorizationOverride", "outputSubcategorization")] = InterfaceDirection.Write, // dead (subcategorization)

            [("PhonologicalSubrule", "excludedMPRFeatures")] = InterfaceDirection.Read,
            [("PhonologicalSubrule", "requiredMPRFeatures")] = InterfaceDirection.Read,
            [("PhonologicalSubrule", "requiredPartsOfSpeech")] = InterfaceDirection.Read,

            [("Segment", "segment")] = InterfaceDirection.Ref,
            [("Segments", "characterDefinitionTable")] = InterfaceDirection.Ref,
            [("SimpleContext", "naturalClass")] = InterfaceDirection.Ref,
            [("Slot", "morphologicalRules")] = InterfaceDirection.Ref,

            // Configures the StemName DEFINITION's own applicability domain (which POS a region covers),
            // not a per-derivation write/read of the word's current POS -- unlike Allomorph.stemName,
            // which labels one specific allomorph instance and is what requiredStemName actually reads.
            [("StemName", "partsOfSpeech")] = InterfaceDirection.Ref,

            [("Stratum", "characterDefinitionTable")] = InterfaceDirection.Ref,
            [("Stratum", "morphologicalRules")] = InterfaceDirection.Ref,
            [("Stratum", "phonologicalRules")] = InterfaceDirection.Ref,
            [("SymbolicFeature", "defaultSymbol")] = InterfaceDirection.Ref,
            [("VariableFeature", "phonologicalFeature")] = InterfaceDirection.Ref,
        };

    /// <summary>
    /// Every (element, attribute) pair the table above must classify -- one per declared IDREF/IDREFS
    /// interface, so a DTD change that adds or removes one is a loud <see cref="Classify"/> failure
    /// rather than a silent fallback.
    /// </summary>
    public static IReadOnlyCollection<(string Element, string Attribute)> ClassifiedInterfaces => Table.Keys.ToArray();

    public static InterfaceDirection Classify(string element, string attribute)
    {
        if (Table.TryGetValue((element, attribute), out InterfaceDirection direction))
            return direction;

        throw new InvalidOperationException(
            $"no semantic direction recorded for {element}.{attribute} -- add it to "
                + $"{nameof(SemanticInterfaceDirection)}.{nameof(Table)} rather than falling back to a guess"
        );
    }
}

/// <summary>
/// The DTD/engine-verified target payload type for a declared Write/Read interface that no fixture
/// currently exercises. <see cref="InterfaceInventoryLedger"/> resolves target types by looking up an
/// attribute's IDREF(S) tokens against a real fixture's own id-to-element map, which only works when
/// some fixture sets a non-empty value -- exactly the interfaces this layer most needs to see are the
/// ones with none. The DTD's own prose comments cannot fill that gap (they are inconsistent -- compare
/// <c>CopyFromInput.index</c>'s comment, which names the wrong element entirely, against the corpus-
/// resolved truth), so every entry here was instead confirmed against the engine: the C# field type
/// each attribute loads into, and, for the two Read entries, the actual gate that consumes it.
/// </summary>
internal static class UnexercisedInterfaceDeclaredPayloadTypes
{
    // CompoundingRule.headProdRestrictionsMprFeatures is corpus-exercised and already resolves to
    // MorphologicalPhonologicalRuleFeature. Its nonHead and output siblings load through the exact
    // same mechanism -- XmlLanguageLoader.LoadMprFeatures into a MprFeatureSet field -- and are read
    // (AnalysisCompoundingRule.CompoundMprFeaturesMatch) / written (SynthesisCompoundingRule's
    // outWord.MprFeatures.AddOutput(_rule.OutputProdRestrictionsMprFeatures)) exactly like their
    // exercised sibling; only the corpus never sets them. PhonologicalSubrule's pair loads through the
    // same LoadMprFeatures call and is read by SynthesisRewriteSubruleSpec's RequiredMprFeatures /
    // ExcludedMprFeatures gate -- this is the literal construct the whole chain layer exists to test.
    private static readonly IReadOnlyDictionary<(string Element, string Attribute), string> Table = new Dictionary<
        (string, string),
        string
    >
    {
        [("CompoundingRule", "nonHeadProdRestrictionsMprFeatures")] = "MorphologicalPhonologicalRuleFeature",
        [("CompoundingRule", "outputProdRestrictionsMprFeatures")] = "MorphologicalPhonologicalRuleFeature",
        [("PhonologicalSubrule", "requiredMPRFeatures")] = "MorphologicalPhonologicalRuleFeature",
        [("PhonologicalSubrule", "excludedMPRFeatures")] = "MorphologicalPhonologicalRuleFeature",
    };

    public static string? Lookup(string element, string attribute) =>
        Table.TryGetValue((element, attribute), out string? type) ? type : null;
}

/// <summary>
/// A construct that can alter an already-written payload before a later reader gates on it. The only
/// known instance anywhere in the grammar is
/// <c>MorphologicalPhonologicalRuleFeatureGroup</c>: <see cref="MprFeatureSet.AddOutput"/>
/// drops every other member of a group whose <c>outputType</c> is (or, per
/// <c>XmlLanguageLoader.GetGroupOutput</c>'s fallthrough, defaults to) <c>overwrite</c> before unioning
/// in a new write. PartOfSpeech has no analogous entry: every writer applies its value with a single
/// FeatureStruct <c>PriorityUnion</c> (e.g. <c>SynthesisCompoundingRule</c>), never through a separate
/// group construct with its own overwrite/append toggle -- so <see cref="MutableIds"/> is empty for
/// every payload type except the one registered below.
/// </summary>
internal static class MutatingConstructs
{
    public static IReadOnlySet<string> MutableIds(string payloadType, XDocument grammar)
    {
        ArgumentNullException.ThrowIfNull(payloadType);
        ArgumentNullException.ThrowIfNull(grammar);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (payloadType != "MorphologicalPhonologicalRuleFeature")
            return ids;

        foreach (XElement group in grammar.Descendants("MorphologicalPhonologicalRuleFeatureGroup"))
        {
            // GetGroupOutput treats anything other than the literal string "append" as overwrite
            // (including a missing attribute, matching the DTD's own "overwrite" default) -- mirrored
            // here rather than only checking for the literal string "overwrite".
            string outputType = (string?)group.Attribute("outputType") ?? "overwrite";
            if (outputType == "append")
                continue;

            string? features = (string?)group.Attribute("features");
            if (string.IsNullOrEmpty(features))
                continue;

            foreach (string id in features.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                ids.Add(id);
        }

        return ids;
    }
}

/// <summary>
/// A payload type reached by at least one <see cref="InterfaceDirection.Write"/> declared interface and
/// at least one <see cref="InterfaceDirection.Read"/> declared interface, per
/// <see cref="SemanticInterfaceDirection"/> -- the semantic replacement for
/// <see cref="InterfaceInventoryLedger.ComputeJunctions"/>, which the same field-grammar evidence that
/// motivated <see cref="SemanticInterfaceDirection"/> shows undercounting: it never asks the
/// writer/reader question of a payload type it has not already found a writer AND a reader for by
/// attribute name, so <c>StemName</c> (written by <c>Allomorph.stemName</c>, read by
/// <c>MorphologicalRule.requiredStemName</c>) never surfaces there at all.
/// </summary>
public sealed record ChainJunction(
    string PayloadType,
    IReadOnlyList<(string Element, string Attribute)> Writers,
    IReadOnlyList<(string Element, string Attribute)> Readers
);

/// <summary>
/// The checked-in denominator for the interaction-chain layer: one row per (writer edge, payload type,
/// reader edge) at each <see cref="ChainJunction"/>. Unlike the edge ledger, whose rows are DTD-declared
/// interfaces one at a time, a row here is a PATH: the composition an edge test cannot see, because
/// passing both edges in isolation says nothing about what happens to the payload between them, the
/// way an intervening <see cref="MutatingConstructs"/> overwrite can. The denominator is junctions x their
/// writers x their readers, taken from the DTD/engine (<see cref="SemanticInterfaceDirection"/> decides
/// which declared interfaces are writers/readers at all; <see cref="UnexercisedInterfaceDeclaredPayloadTypes"/>
/// fills in the payload type for the ones no fixture exercises, so an entirely-untested reader cannot
/// silently vanish from its own denominator); only "exercised" and "hazardous" move with the corpus.
/// </summary>
public static class InteractionChainLedger
{
    public const string RelativePath = "conformance/interaction-chains.tsv";

    private const int ColumnCount = 8;

    public sealed record Row(
        string WriterElement,
        string WriterAttribute,
        string PayloadType,
        string ReaderElement,
        string ReaderAttribute,
        bool Exercised,
        IReadOnlyList<string> ExercisingFixtures,
        bool Hazardous
    );

    private sealed record FixtureIndex(
        string FixtureId,
        IReadOnlyDictionary<(string Element, string Attribute), HashSet<string>> IdsByInterface,
        IReadOnlyDictionary<string, IReadOnlySet<string>> MutableIdsByPayload
    );

    /// <summary>
    /// Groups every declared interface by payload type using <see cref="SemanticInterfaceDirection"/>
    /// (not <see cref="InterfaceInventoryLedger.Row.Direction"/>), then keeps only payload types reached
    /// by at least one writer AND at least one reader.
    /// </summary>
    public static IReadOnlyList<ChainJunction> ComputeJunctions(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);

        (var writersByPayload, var readersByPayload, _) = GroupDeclaredInterfacesByPayload(
            InterfaceInventoryLedger.Compute(repositoryRoot)
        );

        return writersByPayload
            .Keys.Where(readersByPayload.ContainsKey)
            .OrderBy(t => t, StringComparer.Ordinal)
            .Select(t => new ChainJunction(t, writersByPayload[t], readersByPayload[t]))
            .ToArray();
    }

    private static (
        Dictionary<string, List<(string, string)>> WritersByPayload,
        Dictionary<string, List<(string, string)>> ReadersByPayload,
        IReadOnlyDictionary<(string Element, string Attribute), IReadOnlyList<string>> DeclaredTypes
    ) GroupDeclaredInterfacesByPayload(IReadOnlyList<InterfaceInventoryLedger.Row> edgeRows)
    {
        var declaredTypes = new Dictionary<(string Element, string Attribute), IReadOnlyList<string>>();
        foreach (InterfaceInventoryLedger.Row row in edgeRows)
        {
            InterfaceDirection direction = SemanticInterfaceDirection.Classify(row.Element, row.Attribute);
            if (direction == InterfaceDirection.Ref)
                continue;

            if (row.ObservedTargetTypes.Count > 0)
            {
                declaredTypes[(row.Element, row.Attribute)] = row.ObservedTargetTypes;
                continue;
            }

            string? declared = UnexercisedInterfaceDeclaredPayloadTypes.Lookup(row.Element, row.Attribute);
            if (declared != null)
                declaredTypes[(row.Element, row.Attribute)] = new[] { declared };
        }

        var writersByPayload = GroupByPayload(edgeRows, declaredTypes, InterfaceDirection.Write);
        var readersByPayload = GroupByPayload(edgeRows, declaredTypes, InterfaceDirection.Read);
        return (writersByPayload, readersByPayload, declaredTypes);
    }

    /// <summary>
    /// Enumerates every (writer, payload, reader) chain at each <see cref="ChainJunction"/>, then checks
    /// each one against every fixture: a chain is exercised in a fixture when the SAME id appears in
    /// both the writer's and the reader's attribute values there (a stronger claim than "both attributes
    /// are non-empty somewhere in the grammar", matching the level of rigor
    /// <see cref="InterfaceInventoryLedger"/> already applies to a single edge), and hazardous when at
    /// least one exercising fixture also has a <see cref="MutatingConstructs"/> entry covering that
    /// shared id.
    /// </summary>
    public static IReadOnlyList<Row> Compute(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);

        IReadOnlyList<InterfaceInventoryLedger.Row> edgeRows = InterfaceInventoryLedger.Compute(repositoryRoot);
        (var writersByPayload, var readersByPayload, var declaredTypes) = GroupDeclaredInterfacesByPayload(edgeRows);

        IReadOnlyList<ChainJunction> junctions = writersByPayload
            .Keys.Where(readersByPayload.ContainsKey)
            .OrderBy(t => t, StringComparer.Ordinal)
            .Select(t => new ChainJunction(t, writersByPayload[t], readersByPayload[t]))
            .ToArray();

        IReadOnlyList<FixtureIndex> fixtureIndexes = IndexFixtures(
            repositoryRoot,
            declaredTypes.Keys,
            junctions.Select(j => j.PayloadType).ToArray()
        );

        var rows = new List<Row>();
        foreach (ChainJunction junction in junctions)
        {
            IReadOnlyList<(string Element, string Attribute)> writers = junction.Writers;
            IReadOnlyList<(string Element, string Attribute)> readers = junction.Readers;

            foreach ((string writerElement, string writerAttribute) in writers)
            {
                foreach ((string readerElement, string readerAttribute) in readers)
                {
                    var exercisingFixtures = new List<string>();
                    bool hazardous = false;
                    foreach (FixtureIndex fixture in fixtureIndexes)
                    {
                        if (
                            !fixture.IdsByInterface.TryGetValue(
                                (writerElement, writerAttribute),
                                out HashSet<string>? writerIds
                            )
                            || !fixture.IdsByInterface.TryGetValue(
                                (readerElement, readerAttribute),
                                out HashSet<string>? readerIds
                            )
                        )
                        {
                            continue;
                        }

                        var shared = new HashSet<string>(writerIds, StringComparer.Ordinal);
                        shared.IntersectWith(readerIds);
                        if (shared.Count == 0)
                            continue;

                        exercisingFixtures.Add(fixture.FixtureId);
                        if (
                            fixture.MutableIdsByPayload.TryGetValue(
                                junction.PayloadType,
                                out IReadOnlySet<string>? mutable
                            ) && shared.Any(mutable.Contains)
                        )
                        {
                            hazardous = true;
                        }
                    }

                    rows.Add(
                        new Row(
                            writerElement,
                            writerAttribute,
                            junction.PayloadType,
                            readerElement,
                            readerAttribute,
                            exercisingFixtures.Count > 0,
                            exercisingFixtures.OrderBy(f => f, StringComparer.Ordinal).ToArray(),
                            hazardous
                        )
                    );
                }
            }
        }

        return rows.OrderBy(r => r.PayloadType, StringComparer.Ordinal)
            .ThenBy(r => r.WriterElement, StringComparer.Ordinal)
            .ThenBy(r => r.WriterAttribute, StringComparer.Ordinal)
            .ThenBy(r => r.ReaderElement, StringComparer.Ordinal)
            .ThenBy(r => r.ReaderAttribute, StringComparer.Ordinal)
            .ToArray();
    }

    private static Dictionary<string, List<(string Element, string Attribute)>> GroupByPayload(
        IReadOnlyList<InterfaceInventoryLedger.Row> edgeRows,
        IReadOnlyDictionary<(string Element, string Attribute), IReadOnlyList<string>> declaredTypes,
        InterfaceDirection direction
    )
    {
        var byPayload = new Dictionary<string, List<(string, string)>>(StringComparer.Ordinal);
        foreach (InterfaceInventoryLedger.Row row in edgeRows)
        {
            if (SemanticInterfaceDirection.Classify(row.Element, row.Attribute) != direction)
                continue;
            if (!declaredTypes.TryGetValue((row.Element, row.Attribute), out IReadOnlyList<string>? types))
                continue;

            foreach (string type in types)
            {
                if (!byPayload.TryGetValue(type, out List<(string, string)>? list))
                {
                    list = new List<(string, string)>();
                    byPayload[type] = list;
                }
                list.Add((row.Element, row.Attribute));
            }
        }
        return byPayload;
    }

    private static IReadOnlyList<FixtureIndex> IndexFixtures(
        string repositoryRoot,
        IEnumerable<(string Element, string Attribute)> declaredInterfaces,
        IReadOnlyList<string> payloadTypes
    )
    {
        IReadOnlyList<(string Element, string Attribute)> interfaces = declaredInterfaces.ToArray();

        var indexes = new List<FixtureIndex>();
        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            XDocument grammar = XDocument.Load(fixture.GrammarPath);

            var idsByInterface = new Dictionary<(string, string), HashSet<string>>();
            foreach ((string element, string attribute) in interfaces)
            {
                foreach (XElement owner in grammar.Descendants(element))
                {
                    string? value = (string?)owner.Attribute(attribute);
                    if (string.IsNullOrEmpty(value))
                        continue;

                    var key = (element, attribute);
                    if (!idsByInterface.TryGetValue(key, out HashSet<string>? ids))
                    {
                        ids = new HashSet<string>(StringComparer.Ordinal);
                        idsByInterface[key] = ids;
                    }
                    foreach (string token in value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                        ids.Add(token);
                }
            }

            var mutableIdsByPayload = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);
            foreach (string payloadType in payloadTypes)
                mutableIdsByPayload[payloadType] = MutatingConstructs.MutableIds(payloadType, grammar);

            indexes.Add(new FixtureIndex(fixture.Id, idsByInterface, mutableIdsByPayload));
        }

        return indexes;
    }

    public static void Write(string repositoryRoot, IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(rows);
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(path, ToText(rows));
    }

    /// <summary>
    /// Renders the ledger deterministically (payload type, then writer, then reader) so two runs over
    /// an unchanged DTD/engine/corpus byte-for-byte agree -- the drift gate depends on this.
    /// </summary>
    public static string ToText(IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var writer = new StringWriter();
        writer.WriteLine("# GENERATED by hc-conformance --write-interaction-chains. One row per (writer edge, payload");
        writer.WriteLine("# type, reader edge) at each ChainJunction -- a payload type reached by both a write- and a");
        writer.WriteLine(
            "# read-direction declared interface, per SemanticInterfaceDirection's engine-checked judgment"
        );
        writer.WriteLine(
            "# of what each interface does (NOT interface-inventory.tsv's own name-prefix heuristic, which"
        );
        writer.WriteLine(
            "# a real field grammar disproved -- see SemanticInterfaceDirection's doc comment). payload_type"
        );
        writer.WriteLine(
            "# also covers interfaces no fixture exercises yet (see UnexercisedInterfaceDeclaredPayloadTypes),"
        );
        writer.WriteLine("# so an entirely-untested reader still appears in its own denominator instead of silently");
        writer.WriteLine("# vanishing. exercising_fixtures lists every fixture where the SAME id appears in both the");
        writer.WriteLine(
            "# writer's and the reader's attribute values; hazardous means at least one of those fixtures"
        );
        writer.WriteLine("# also has a MutatingConstructs entry (today: an overwrite-type");
        writer.WriteLine("# MorphologicalPhonologicalRuleFeatureGroup) covering that id.");
        writer.WriteLine(
            "writer_element\twriter_attribute\tpayload_type\treader_element\treader_attribute\texercised\texercising_fixtures\thazardous"
        );
        foreach (
            Row row in rows.OrderBy(r => r.PayloadType, StringComparer.Ordinal)
                .ThenBy(r => r.WriterElement, StringComparer.Ordinal)
                .ThenBy(r => r.WriterAttribute, StringComparer.Ordinal)
                .ThenBy(r => r.ReaderElement, StringComparer.Ordinal)
                .ThenBy(r => r.ReaderAttribute, StringComparer.Ordinal)
        )
        {
            writer.WriteLine(
                string.Join(
                    '\t',
                    row.WriterElement,
                    row.WriterAttribute,
                    row.PayloadType,
                    row.ReaderElement,
                    row.ReaderAttribute,
                    row.Exercised ? "yes" : "no",
                    string.Join(",", row.ExercisingFixtures),
                    row.Hazardous ? "yes" : "no"
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
                || line.StartsWith("writer_element\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields.Length != ColumnCount)
                throw new FormatException($"{RelativePath}: '{line}' must be {ColumnCount} tab-separated fields");
            if (fields[5] is not ("yes" or "no"))
                throw new FormatException($"{RelativePath}: unknown exercised flag '{fields[5]}'");
            if (fields[7] is not ("yes" or "no"))
                throw new FormatException($"{RelativePath}: unknown hazardous flag '{fields[7]}'");

            IReadOnlyList<string> fixtures = fields[6].Length == 0 ? Array.Empty<string>() : fields[6].Split(',');
            rows.Add(
                new Row(
                    fields[0],
                    fields[1],
                    fields[2],
                    fields[3],
                    fields[4],
                    fields[5] == "yes",
                    fixtures,
                    fields[7] == "yes"
                )
            );
        }

        return rows;
    }
}
