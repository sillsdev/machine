using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class TemplateMaskedProofsTests
{
    // Three slots, all of whose rules are members of the unordered Stratum's own morphologicalRules list
    // -- the shape that certifies both s1~s2 and s2~s3.
    private const string FullyMaskedGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="m1 m2 m3" morphologicalRuleOrder="unordered"><Name>S</Name>
            <AffixTemplates><AffixTemplate><Name>tmpl</Name>
              <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
              <Slot morphologicalRules="m2"><Name>s2</Name></Slot>
              <Slot morphologicalRules="m3"><Name>s3</Name></Slot>
            </AffixTemplate></AffixTemplates>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string LinearGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="m1 m2 m3" morphologicalRuleOrder="linear"><Name>S</Name>
            <AffixTemplates><AffixTemplate><Name>tmpl</Name>
              <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
              <Slot morphologicalRules="m2"><Name>s2</Name></Slot>
              <Slot morphologicalRules="m3"><Name>s3</Name></Slot>
            </AffixTemplate></AffixTemplates>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string AbsentOrderGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="m1 m2 m3"><Name>S</Name>
            <AffixTemplates><AffixTemplate><Name>tmpl</Name>
              <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
              <Slot morphologicalRules="m2"><Name>s2</Name></Slot>
              <Slot morphologicalRules="m3"><Name>s3</Name></Slot>
            </AffixTemplate></AffixTemplates>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    // s3's rule m3 is NOT in the cascade -- the whole template is not reproducible, so even the s1~s2
    // pair (which never touches m3) must be rejected: precondition (c) is about every Slot, not the pair.
    private const string OtherSlotEscapesCascadeGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="m1 m2" morphologicalRuleOrder="unordered"><Name>S</Name>
            <AffixTemplates><AffixTemplate><Name>tmpl</Name>
              <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
              <Slot morphologicalRules="m2"><Name>s2</Name></Slot>
              <Slot morphologicalRules="m3"><Name>s3</Name></Slot>
            </AffixTemplate></AffixTemplates>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    [Test]
    public void BuildsAProofForAnAdjacentSlotPairFullyReproducedByTheCascade()
    {
        XDocument grammar = XDocument.Parse(FullyMaskedGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.MemberA == "s1" && i.MemberB == "s2");

        Proof? proof = TemplateMaskedProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Kind, Is.EqualTo(TemplateMaskedProofs.Kind));
        Assert.That(proof.Check, Does.Contain("unordered"));
        Assert.That(TemplateMaskedProofs.Verify(grammar, "fx", proof), Is.True);
    }

    [Test]
    public void BuildsAProofForTheOtherAdjacentSlotPairInTheSameTemplate()
    {
        XDocument grammar = XDocument.Parse(FullyMaskedGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.MemberA == "s2" && i.MemberB == "s3");

        Assert.That(TemplateMaskedProofs.TryBuild(grammar, item), Is.Not.Null);
    }

    // Guard: a linear stratum must never license this proof kind -- linear order is exactly the case
    // where slot position DOES matter.
    [Test]
    public void RefusesWhenTheOwningStratumIsLinear()
    {
        XDocument grammar = XDocument.Parse(LinearGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.MemberA == "s1" && i.MemberB == "s2");

        Assert.That(TemplateMaskedProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: an absent morphologicalRuleOrder resolves to the DTD default "linear", never treated as unordered.
    [Test]
    public void RefusesWhenMorphologicalRuleOrderIsAbsent()
    {
        XDocument grammar = XDocument.Parse(AbsentOrderGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.MemberA == "s1" && i.MemberB == "s2");

        Assert.That(TemplateMaskedProofs.TryBuild(grammar, item), Is.Null);
    }

    // The precondition-(c) case: a rule referenced by a slot OUTSIDE the swapped pair is missing from the
    // cascade, so the whole template is a genuinely distinct derivation path and the pair itself -- which
    // never references the missing rule -- must still be rejected.
    [Test]
    public void RefusesWhenAnyOtherSlotInTheTemplateReferencesARuleMissingFromTheCascade()
    {
        XDocument grammar = XDocument.Parse(OtherSlotEscapesCascadeGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.MemberA == "s1" && i.MemberB == "s2");

        Assert.That(TemplateMaskedProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: this kind never fires for a non-Slot item, even on an unordered Stratum whose morphologicalRules
    // pair would satisfy UnorderedInvariantProofs.
    [Test]
    public void RefusesForAStratumMorphologicalRulesPair()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language><Strata>
              <Stratum morphologicalRules="m1 m2" morphologicalRuleOrder="unordered"><Name>S</Name></Stratum>
            </Strata></Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(TemplateMaskedProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: a Slot with no morphologicalRules attribute at all cannot be verified reproducible -- fail
    // closed rather than treating "no attribute" as "no rules to worry about".
    [Test]
    public void RefusesWhenASlotDeclaresNoMorphologicalRulesAttribute()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language><Strata>
              <Stratum morphologicalRules="m1 m2" morphologicalRuleOrder="unordered"><Name>S</Name>
                <AffixTemplates><AffixTemplate><Name>tmpl</Name>
                  <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
                  <Slot><Name>s2</Name></Slot>
                </AffixTemplate></AffixTemplates>
              </Stratum>
            </Strata></Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.Kind == OrderingListKind.AffixTemplateSlots);

        Assert.That(TemplateMaskedProofs.TryBuild(grammar, item), Is.Null);
    }

    // The core rejection scenario the task asks for: a proof built while the stratum was unordered must be
    // REJECTED once the stratum is flipped to linear, because Verify recomputes rather than trusts.
    [Test]
    public void VerifyRejectsAProofOnceTheStratumIsFlippedToLinear()
    {
        XDocument original = XDocument.Parse(FullyMaskedGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(original, "fx")
            .Single(i => i.MemberA == "s1" && i.MemberB == "s2");
        Proof proof = TemplateMaskedProofs.TryBuild(original, item)!;

        XDocument flipped = XDocument.Parse(LinearGrammar);

        Assert.That(TemplateMaskedProofs.Verify(flipped, "fx", proof), Is.False);
    }

    // The second stale-proof scenario: a proof built while every slot's rules were in the cascade must be
    // REJECTED once one of those rules is removed from the Stratum's morphologicalRules list.
    [Test]
    public void VerifyRejectsAProofOnceARuleIsRemovedFromTheCascade()
    {
        XDocument original = XDocument.Parse(FullyMaskedGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(original, "fx")
            .Single(i => i.MemberA == "s1" && i.MemberB == "s2");
        Proof proof = TemplateMaskedProofs.TryBuild(original, item)!;

        XDocument shrunkCascade = XDocument.Parse(OtherSlotEscapesCascadeGrammar);

        Assert.That(TemplateMaskedProofs.Verify(shrunkCascade, "fx", proof), Is.False);
    }

    [Test]
    public void VerifyRejectsAStaleProofWhoseItemNoLongerExists()
    {
        XDocument grammar = XDocument.Parse(FullyMaskedGrammar);
        var stale = new Proof("ordering:fx/slots/gone~alsoGone", TemplateMaskedProofs.Kind, "stale");

        Assert.That(TemplateMaskedProofs.Verify(grammar, "fx", stale), Is.False);
    }
}
