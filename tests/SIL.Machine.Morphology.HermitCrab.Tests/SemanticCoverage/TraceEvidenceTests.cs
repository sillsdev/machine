using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class TraceEvidenceTests
{
    private const string Dtd = "<!ELEMENT Root EMPTY>";

    private static SemanticInventory InventoryWith(params string[] surfaceIds)
    {
        SemanticInventory basis = SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("f.dtd", Dtd));
        return basis with
        {
            Surfaces = surfaceIds.Select(id => new InventorySurface(id, "enum", id, null, "f.dtd")).ToArray(),
        };
    }

    private static XDocument Grammar(string ruleAttributes) =>
        XDocument.Parse(
            $"<Strata><Stratum><MorphologicalRuleDefinitions>"
                + $"<MorphologicalRule id=\"mrLive\" {ruleAttributes}><Name>n</Name></MorphologicalRule>"
                + $"</MorphologicalRuleDefinitions></Stratum></Strata>"
        );

    [Test]
    public void AnAttributeOnAFiringRuleIsTraceEvidence()
    {
        SemanticInventory inventory = InventoryWith("dtd:enum/MorphologicalRule/blockable/true");

        IReadOnlyList<SurfaceEvidence> graded = TraceEvidence.Grade(
            "fx",
            Grammar("blockable=\"true\""),
            inventory,
            new[] { "mrLive" },
            hasVerifiedParse: true,
            Array.Empty<string>()
        );

        Assert.That(graded.Single().Strength, Is.EqualTo(EvidenceStrength.Trace));
    }

    // The whole point of trace grading: a rule can be declared, load fine, and never run.
    [Test]
    public void AnAttributeOnARuleThatNeverFiredIsOnlyPresence()
    {
        SemanticInventory inventory = InventoryWith("dtd:enum/MorphologicalRule/blockable/true");

        IReadOnlyList<SurfaceEvidence> graded = TraceEvidence.Grade(
            "fx",
            Grammar("blockable=\"true\""),
            inventory,
            Array.Empty<string>(),
            hasVerifiedParse: true,
            Array.Empty<string>()
        );

        Assert.That(graded.Single().Strength, Is.EqualTo(EvidenceStrength.Presence));
        Assert.That(graded.Single().Detail, Does.Contain("mrLive"));
    }

    [Test]
    public void ADeactivatedDeclarationNeedsAWordThatNamesItToCountAsANegativeControl()
    {
        SemanticInventory inventory = InventoryWith("dtd:enum/MorphologicalRule/isActive/no");
        XDocument grammar = Grammar("isActive=\"no\"");

        Assert.That(
            TraceEvidence.Grade("fx", grammar, inventory, Array.Empty<string>(), true, new[] { "mrLive" })
                .Single().Strength,
            Is.EqualTo(EvidenceStrength.NegativeControl)
        );
        Assert.That(
            TraceEvidence.Grade("fx", grammar, inventory, Array.Empty<string>(), true, Array.Empty<string>())
                .Single().Strength,
            Is.EqualTo(EvidenceStrength.Presence),
            "a decoy no word names is not evidence"
        );
    }

    [Test]
    public void AFixtureWithNoVerifiedParseCannotReachStructuralStrength()
    {
        SemanticInventory inventory = InventoryWith("dtd:enum/Stratum/cyclicity/cyclic");
        var grammar = XDocument.Parse("<Strata><Stratum cyclicity=\"cyclic\"><Name>n</Name></Stratum></Strata>");

        Assert.That(
            TraceEvidence.Grade("fx", grammar, inventory, Array.Empty<string>(), hasVerifiedParse: false, Array.Empty<string>())
                .Single().Strength,
            Is.EqualTo(EvidenceStrength.Presence)
        );
        Assert.That(
            TraceEvidence.Grade("fx", grammar, inventory, Array.Empty<string>(), hasVerifiedParse: true, Array.Empty<string>())
                .Single().Strength,
            Is.EqualTo(EvidenceStrength.Structural)
        );
    }

    [Test]
    public void OwningRuleIsTheNearestIdentifiedRuleAncestor()
    {
        var grammar = XDocument.Parse(
            "<MorphologicalRule id=\"outer\"><MorphologicalSubrules><MorphologicalSubrule id=\"inner\">"
                + "<MorphologicalOutput /></MorphologicalSubrule></MorphologicalSubrules></MorphologicalRule>"
        );
        XElement output = grammar.Descendants("MorphologicalOutput").Single();

        // MorphologicalSubrule ids are not fired-rule ids, so attribution walks past them.
        Assert.That(TraceEvidence.OwningRuleId(output), Is.EqualTo("outer"));
        Assert.That(TraceEvidence.DeactivatedAncestor(output), Is.Null);
    }

    [Test]
    public void DeactivationIsInheritedByDescendants()
    {
        var grammar = XDocument.Parse(
            "<LexicalEntries><LexicalEntry id=\"e\" isActive=\"no\"><Allomorphs>"
                + "<Allomorph id=\"a\"><PhoneticShape>x</PhoneticShape></Allomorph></Allomorphs></LexicalEntry></LexicalEntries>"
        );
        XElement shape = grammar.Descendants("PhoneticShape").Single();

        Assert.That(TraceEvidence.DeactivatedAncestor(shape), Is.EqualTo("e"));
    }
}
