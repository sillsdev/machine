using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class GrammarMutatorTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    private static SemanticInventory Inventory() => GrammarCoverageGate.ReadInventory(RepositoryRoot());

    [Test]
    public void DeletingAnElementRemovesEveryOccurrence()
    {
        var grammar = XDocument.Parse(
            "<Strata><Stratum><Name>a</Name><LexicalEntries>"
                + "<LexicalEntry id=\"e1\"><Allomorphs><Allomorph id=\"a1\"><PhoneticShape>x</PhoneticShape></Allomorph></Allomorphs></LexicalEntry>"
                + "<LexicalEntry id=\"e2\"><Allomorphs><Allomorph id=\"a2\"><PhoneticShape>y</PhoneticShape></Allomorph></Allomorphs></LexicalEntry>"
                + "</LexicalEntries></Stratum></Strata>"
        );

        GrammarMutation? mutation = GrammarMutator.Mutate(grammar, "dtd:element/LexicalEntry", Inventory());

        Assert.That(mutation, Is.Not.Null);
        Assert.That(mutation!.Kind, Is.EqualTo(GrammarMutator.DeletedElements));
        Assert.That(mutation.Mutated.Descendants("LexicalEntry"), Is.Empty);
        Assert.That(mutation.Detail, Does.Contain("2"));
        // The mutation must not touch the original.
        Assert.That(grammar.Descendants("LexicalEntry").Count(), Is.EqualTo(2));
    }

    [Test]
    public void ANonDefaultEnumValueIsRewrittenToADeclaredSibling()
    {
        var grammar = XDocument.Parse("<Strata><Stratum cyclicity=\"cyclic\"><Name>a</Name></Stratum></Strata>");

        GrammarMutation? mutation = GrammarMutator.Mutate(grammar, "dtd:enum/Stratum/cyclicity/cyclic", Inventory());

        Assert.That(mutation, Is.Not.Null);
        Assert.That(mutation!.Kind, Is.EqualTo(GrammarMutator.RewroteAttribute));
        Assert.That(
            (string?)mutation.Mutated.Descendants("Stratum").Single().Attribute("cyclicity"),
            Is.EqualTo("noncyclic"),
            "the sibling must be a value the DTD actually declares"
        );
    }

    [Test]
    public void ASurfaceTheDocumentDoesNotContainYieldsNoMutation()
    {
        var grammar = XDocument.Parse("<Strata><Stratum><Name>a</Name></Stratum></Strata>");

        Assert.That(GrammarMutator.Mutate(grammar, "dtd:element/CompoundingRule", Inventory()), Is.Null);
        Assert.That(GrammarMutator.Mutate(grammar, "dtd:enum/Stratum/cyclicity/cyclic", Inventory()), Is.Null);
    }

    // redupMorphType declares exactly three values (implicit/prefix/suffix), so mutating away from
    // "suffix" has exactly two siblings to enumerate -- the shape of the proven false-negative this
    // enumeration exists to fix (see CounterfactualGateTests for the end-to-end case).
    [Test]
    public void MutateEnumAgainstEverySiblingBuildsOneMutationPerDeclaredSiblingInOrdinalOrder()
    {
        var grammar = XDocument.Parse("<Root><MorphologicalOutput redupMorphType=\"suffix\" /></Root>");

        IReadOnlyList<GrammarMutator.EnumSiblingCandidate> candidates = GrammarMutator.MutateEnumAgainstEverySibling(
            grammar,
            "dtd:enum/MorphologicalOutput/redupMorphType/suffix",
            Inventory()
        );

        Assert.That(candidates.Select(c => c.Sibling), Is.EqualTo(new[] { "implicit", "prefix" }));
        foreach (GrammarMutator.EnumSiblingCandidate candidate in candidates)
        {
            Assert.That(
                (string?)
                    candidate.Mutation.Mutated.Descendants("MorphologicalOutput").Single().Attribute("redupMorphType"),
                Is.EqualTo(candidate.Sibling)
            );
        }
        // The original document must not be touched by building the candidates.
        Assert.That(
            (string?)grammar.Descendants("MorphologicalOutput").Single().Attribute("redupMorphType"),
            Is.EqualTo("suffix")
        );
    }

    [Test]
    public void MutateEnumAgainstEverySiblingIsDeterministicAcrossRepeatedCalls()
    {
        var grammar = XDocument.Parse("<Root><MorphologicalOutput redupMorphType=\"suffix\" /></Root>");
        SemanticInventory inventory = Inventory();

        string[] Siblings() =>
            GrammarMutator
                .MutateEnumAgainstEverySibling(grammar, "dtd:enum/MorphologicalOutput/redupMorphType/suffix", inventory)
                .Select(c => c.Sibling)
                .ToArray();

        string[] first = Siblings();
        string[] second = Siblings();
        Assert.That(second, Is.EqualTo(first), "repeated calls must agree on both the set and the order");
    }

    [Test]
    public void MutateEnumAgainstEverySiblingIsEmptyWhenTheDocumentDoesNotContainTheSurface()
    {
        var grammar = XDocument.Parse("<Root><MorphologicalOutput /></Root>");

        Assert.That(
            GrammarMutator.MutateEnumAgainstEverySibling(
                grammar,
                "dtd:enum/MorphologicalOutput/redupMorphType/suffix",
                Inventory()
            ),
            Is.Empty
        );
    }

    [Test]
    public void MutateEnumAgainstEverySiblingIsEmptyForAnElementSurface()
    {
        var grammar = XDocument.Parse("<Strata><Stratum><Name>a</Name></Stratum></Strata>");

        Assert.That(
            GrammarMutator.MutateEnumAgainstEverySibling(grammar, "dtd:element/Stratum", Inventory()),
            Is.Empty
        );
    }

    [Test]
    public void SiblingSelectionIsDeterministicAndNeverTheValueItself()
    {
        SemanticInventory inventory = Inventory();

        Assert.That(GrammarMutator.Sibling(inventory, "Stratum", "cyclicity", "cyclic"), Is.EqualTo("noncyclic"));
        Assert.That(GrammarMutator.Sibling(inventory, "Stratum", "cyclicity", "noncyclic"), Is.EqualTo("cyclic"));
        Assert.That(
            GrammarMutator.Sibling(inventory, "Stratum", "cyclicity", "cyclic"),
            Is.EqualTo(GrammarMutator.Sibling(inventory, "Stratum", "cyclicity", "cyclic")),
            "repeated calls must agree"
        );
    }

    // Percent-encoding is applied to every ID component, so a Greek variable name has to survive the
    // round trip or its mutation would silently target nothing.
    [Test]
    public void EncodedValuesRoundTripThroughMutation()
    {
        var grammar = XDocument.Parse(
            "<VariableFeatures><VariableFeature id=\"v\" name=\"α\" phonologicalFeature=\"f\" /></VariableFeatures>"
        );

        GrammarMutation? mutation = GrammarMutator.Mutate(grammar, "dtd:enum/VariableFeature/name/%CE%B1", Inventory());

        Assert.That(mutation, Is.Not.Null);
        Assert.That(
            (string?)mutation!.Mutated.Descendants("VariableFeature").Single().Attribute("name"),
            Is.Not.EqualTo("α"),
            "the Greek value must actually be replaced"
        );
    }

    // A minimal, non-DTD-valid fragment mirroring the real fail-fast-IDREF shape: an inactive
    // CharacterDefinitionTable ("tableDecoy") that only an equally inactive Stratum references.
    // GrammarMutator manipulates XDocument directly and never loads via XmlLanguageLoader, so the
    // fragment does not need to be schema-complete -- only the id/attribute shapes matter.
    private const string JointPartnerFragment = """
        <HermitCrabInput>
          <CharacterDefinitionTable id="tableDecoy" isActive="no"><Name>Decoy</Name></CharacterDefinitionTable>
          <CharacterDefinitionTable id="tableLive" isActive="yes"><Name>Live</Name></CharacterDefinitionTable>
          <Stratum characterDefinitionTable="tableDecoy" isActive="no"><Name>DecoyStratum</Name></Stratum>
          <Stratum characterDefinitionTable="tableLive" isActive="yes"><Name>Main</Name></Stratum>
        </HermitCrabInput>
        """;

    [Test]
    public void FindJointPartnerLocatesTheSoleInactiveReferencingDeclaration()
    {
        var grammar = XDocument.Parse(JointPartnerFragment);

        GrammarMutator.JointPartner? partner = GrammarMutator.FindJointPartner(
            grammar,
            "dtd:enum/CharacterDefinitionTable/isActive/no"
        );

        Assert.That(partner, Is.Not.Null);
        Assert.That(partner!.TargetElement, Is.EqualTo("CharacterDefinitionTable"));
        Assert.That(partner.TargetId, Is.EqualTo("tableDecoy"));
        Assert.That(partner.PartnerElement, Is.EqualTo("Stratum"));
        Assert.That(partner.PartnerAttribute, Is.EqualTo("characterDefinitionTable"));
    }

    [Test]
    public void FindJointPartnerReturnsNullWhenNothingReferencesTheInactiveDeclaration()
    {
        // tableLive's own isActive="yes" would need a "no" value to even be eligible, and the
        // decoy table here is never named by anything -- the negative-space half of the fixture.
        var grammar = XDocument.Parse(
            """
            <HermitCrabInput>
              <CharacterDefinitionTable id="tableOrphan" isActive="no"><Name>Orphan</Name></CharacterDefinitionTable>
            </HermitCrabInput>
            """
        );

        Assert.That(GrammarMutator.FindJointPartner(grammar, "dtd:enum/CharacterDefinitionTable/isActive/no"), Is.Null);
    }

    [Test]
    public void FindJointPartnerOnlyAppliesToInactiveEnumSurfaces()
    {
        var grammar = XDocument.Parse(JointPartnerFragment);

        // isActive/yes has no dangling-reference hazard to guard against, and cyclicity is not
        // isActive at all -- neither shape is what a joint mutation exists to fix.
        Assert.That(
            GrammarMutator.FindJointPartner(grammar, "dtd:enum/CharacterDefinitionTable/isActive/yes"),
            Is.Null
        );
        Assert.That(GrammarMutator.FindJointPartner(grammar, "dtd:enum/Stratum/cyclicity/cyclic"), Is.Null);
        Assert.That(GrammarMutator.FindJointPartner(grammar, "dtd:element/Stratum"), Is.Null);
    }

    [Test]
    public void MutatePartnerAloneActivatesOnlyThePartner()
    {
        var grammar = XDocument.Parse(JointPartnerFragment);
        GrammarMutator.JointPartner partner = GrammarMutator.FindJointPartner(
            grammar,
            "dtd:enum/CharacterDefinitionTable/isActive/no"
        )!;

        GrammarMutation? mutation = GrammarMutator.MutatePartnerAlone(grammar, partner);

        Assert.That(mutation, Is.Not.Null);
        Assert.That(mutation!.Kind, Is.EqualTo(GrammarMutator.ActivatedPartnerAlone));
        XElement decoyStratum = mutation
            .Mutated.Descendants("Stratum")
            .First(e => (string?)e.Attribute("characterDefinitionTable") == "tableDecoy");
        Assert.That((string?)decoyStratum.Attribute("isActive"), Is.EqualTo("yes"), "the partner must activate");
        XElement decoyTable = mutation
            .Mutated.Descendants("CharacterDefinitionTable")
            .First(e => (string?)e.Attribute("id") == "tableDecoy");
        Assert.That((string?)decoyTable.Attribute("isActive"), Is.EqualTo("no"), "the target must stay inactive");
        // The original document must not be touched.
        Assert.That(
            (string?)
                grammar
                    .Descendants("Stratum")
                    .First(e => (string?)e.Attribute("characterDefinitionTable") == "tableDecoy")
                    .Attribute("isActive"),
            Is.EqualTo("no")
        );
    }

    [Test]
    public void MutateJointlyActivatesBothTargetAndPartner()
    {
        var grammar = XDocument.Parse(JointPartnerFragment);
        SemanticInventory inventory = Inventory();
        GrammarMutator.JointPartner partner = GrammarMutator.FindJointPartner(
            grammar,
            "dtd:enum/CharacterDefinitionTable/isActive/no"
        )!;

        GrammarMutation? mutation = GrammarMutator.MutateJointly(
            grammar,
            "dtd:enum/CharacterDefinitionTable/isActive/no",
            partner,
            inventory
        );

        Assert.That(mutation, Is.Not.Null);
        Assert.That(mutation!.Kind, Is.EqualTo(GrammarMutator.ActivatedJointly));
        XElement decoyTable = mutation
            .Mutated.Descendants("CharacterDefinitionTable")
            .First(e => (string?)e.Attribute("id") == "tableDecoy");
        XElement decoyStratum = mutation
            .Mutated.Descendants("Stratum")
            .First(e => (string?)e.Attribute("characterDefinitionTable") == "tableDecoy");
        Assert.That((string?)decoyTable.Attribute("isActive"), Is.EqualTo("yes"));
        Assert.That((string?)decoyStratum.Attribute("isActive"), Is.EqualTo("yes"));
        // The original document must not be touched.
        Assert.That(
            (string?)
                grammar
                    .Descendants("CharacterDefinitionTable")
                    .First(e => (string?)e.Attribute("id") == "tableDecoy")
                    .Attribute("isActive"),
            Is.EqualTo("no")
        );
    }

    [Test]
    public void EveryObservableSurfaceInEveryFixtureCanBeMutated()
    {
        string root = RepositoryRoot();
        SemanticInventory inventory = Inventory();
        var unmutatable = new List<string>();

        foreach ((string fixtureId, string grammarPath) in GrammarCoverageGate.DiscoverGrammars(root))
        {
            XDocument grammar = XDocument.Load(grammarPath);
            foreach (string surfaceId in GrammarFeatureUsage.Read(grammar, inventory))
            {
                if (GrammarMutator.Mutate(grammar, surfaceId, inventory) is null)
                    unmutatable.Add($"{fixtureId}: {surfaceId}");
            }
        }

        Assert.That(
            unmutatable,
            Is.Empty,
            $"a surface a fixture contains but the mutator cannot neutralize can never be evidenced:\n  {string.Join("\n  ", unmutatable)}"
        );
    }
}
