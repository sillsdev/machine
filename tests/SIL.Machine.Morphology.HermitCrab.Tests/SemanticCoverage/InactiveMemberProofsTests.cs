using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class InactiveMemberProofsTests
{
    private const string SecondMemberInactiveGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="m1 m2"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="m1"><Name>r1</Name></MorphologicalRule>
              <MorphologicalRule id="m2" isActive="no"><Name>r2</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string BothMembersActiveGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="m1 m2"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="m1"><Name>r1</Name></MorphologicalRule>
              <MorphologicalRule id="m2"><Name>r2</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string InactiveSlotGrammar = """
        <HermitCrabInput><Language><Strata><Stratum><Name>S</Name>
          <AffixTemplates><AffixTemplate><Name>tmpl</Name>
            <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
            <Slot morphologicalRules="m2" isActive="no"><Name>s2</Name></Slot>
          </AffixTemplate></AffixTemplates>
        </Stratum></Strata></Language></HermitCrabInput>
        """;

    // The Slot itself is active; only the single rule it names is inactive, so the Slot resolves to
    // nothing -- the empty-slot case, distinct from InactiveSlotGrammar's own isActive="no" on the Slot.
    private const string EmptySlotOnlyRuleInactiveGrammar = """
        <HermitCrabInput><Language><Strata><Stratum><Name>S</Name>
          <MorphologicalRuleDefinitions>
            <MorphologicalRule id="m1"><Name>r1</Name></MorphologicalRule>
            <MorphologicalRule id="m2" isActive="no"><Name>r2</Name></MorphologicalRule>
          </MorphologicalRuleDefinitions>
          <AffixTemplates><AffixTemplate><Name>tmpl</Name>
            <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
            <Slot morphologicalRules="m2"><Name>s2</Name></Slot>
          </AffixTemplate></AffixTemplates>
        </Stratum></Strata></Language></HermitCrabInput>
        """;

    // m2 reactivated: the same Slot shape as EmptySlotOnlyRuleInactiveGrammar, but s2 now resolves to an
    // active rule -- used to falsify a proof built against the grammar above.
    private const string EmptySlotRuleReactivatedGrammar = """
        <HermitCrabInput><Language><Strata><Stratum><Name>S</Name>
          <MorphologicalRuleDefinitions>
            <MorphologicalRule id="m1"><Name>r1</Name></MorphologicalRule>
            <MorphologicalRule id="m2"><Name>r2</Name></MorphologicalRule>
          </MorphologicalRuleDefinitions>
          <AffixTemplates><AffixTemplate><Name>tmpl</Name>
            <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
            <Slot morphologicalRules="m2"><Name>s2</Name></Slot>
          </AffixTemplate></AffixTemplates>
        </Stratum></Strata></Language></HermitCrabInput>
        """;

    // s2 declares no morphologicalRules attribute at all -- resolves to zero rules just as vacuously as
    // naming only an inactive one.
    private const string SlotWithNoRulesAttributeGrammar = """
        <HermitCrabInput><Language><Strata><Stratum><Name>S</Name>
          <MorphologicalRuleDefinitions>
            <MorphologicalRule id="m1"><Name>r1</Name></MorphologicalRule>
          </MorphologicalRuleDefinitions>
          <AffixTemplates><AffixTemplate><Name>tmpl</Name>
            <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
            <Slot><Name>s2</Name></Slot>
          </AffixTemplate></AffixTemplates>
        </Stratum></Strata></Language></HermitCrabInput>
        """;

    // s2 names only an id that is not declared anywhere in the document.
    private const string SlotNamingOnlyNonexistentRuleGrammar = """
        <HermitCrabInput><Language><Strata><Stratum><Name>S</Name>
          <MorphologicalRuleDefinitions>
            <MorphologicalRule id="m1"><Name>r1</Name></MorphologicalRule>
          </MorphologicalRuleDefinitions>
          <AffixTemplates><AffixTemplate><Name>tmpl</Name>
            <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
            <Slot morphologicalRules="ghost"><Name>s2</Name></Slot>
          </AffixTemplate></AffixTemplates>
        </Stratum></Strata></Language></HermitCrabInput>
        """;

    // Both Slots resolve to an active rule -- must never be certified as empty.
    private const string BothSlotsResolveToAnActiveRuleGrammar = """
        <HermitCrabInput><Language><Strata><Stratum><Name>S</Name>
          <MorphologicalRuleDefinitions>
            <MorphologicalRule id="m1"><Name>r1</Name></MorphologicalRule>
            <MorphologicalRule id="m2"><Name>r2</Name></MorphologicalRule>
          </MorphologicalRuleDefinitions>
          <AffixTemplates><AffixTemplate><Name>tmpl</Name>
            <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
            <Slot morphologicalRules="m2"><Name>s2</Name></Slot>
          </AffixTemplate></AffixTemplates>
        </Stratum></Strata></Language></HermitCrabInput>
        """;

    // s1 names one active and one inactive rule: it can still contribute via m1, so the pair must not
    // be certified even though m2 alone would resolve to nothing.
    private const string SlotWithSomeActiveAndSomeInactiveRulesGrammar = """
        <HermitCrabInput><Language><Strata><Stratum><Name>S</Name>
          <MorphologicalRuleDefinitions>
            <MorphologicalRule id="m1"><Name>r1</Name></MorphologicalRule>
            <MorphologicalRule id="m2" isActive="no"><Name>r2</Name></MorphologicalRule>
            <MorphologicalRule id="m3"><Name>r3</Name></MorphologicalRule>
          </MorphologicalRuleDefinitions>
          <AffixTemplates><AffixTemplate><Name>tmpl</Name>
            <Slot morphologicalRules="m1 m2"><Name>s1</Name></Slot>
            <Slot morphologicalRules="m3"><Name>s2</Name></Slot>
          </AffixTemplate></AffixTemplates>
        </Stratum></Strata></Language></HermitCrabInput>
        """;

    [Test]
    public void BuildsAProofWhenTheSecondMemberIsInactive()
    {
        XDocument grammar = XDocument.Parse(SecondMemberInactiveGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = InactiveMemberProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Kind, Is.EqualTo(InactiveMemberProofs.Kind));
        Assert.That(proof.Check, Does.Contain("m2"));
        Assert.That(InactiveMemberProofs.Verify(grammar, "fx", proof), Is.True);
    }

    [Test]
    public void BuildsAProofForAnInactiveSlotMember()
    {
        XDocument grammar = XDocument.Parse(InactiveSlotGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = InactiveMemberProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Check, Does.Contain("s2"));
    }

    // The empty-slot case: the Slot itself is active, but its only referenced rule is not.
    [Test]
    public void BuildsAProofForASlotWhoseOnlyReferencedRuleIsInactive()
    {
        XDocument grammar = XDocument.Parse(EmptySlotOnlyRuleInactiveGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = InactiveMemberProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Kind, Is.EqualTo(InactiveMemberProofs.Kind));
        Assert.That(proof.Check, Does.Contain("s2"));
        Assert.That(proof.Check, Does.Contain("m2"));
        Assert.That(InactiveMemberProofs.Verify(grammar, "fx", proof), Is.True);
    }

    // A Slot with no morphologicalRules attribute at all resolves to zero rules just as vacuously as one
    // naming only an inactive id.
    [Test]
    public void BuildsAProofForASlotWithNoMorphologicalRulesAttribute()
    {
        XDocument grammar = XDocument.Parse(SlotWithNoRulesAttributeGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = InactiveMemberProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Check, Does.Contain("s2"));
    }

    // A Slot naming only an id that does not exist anywhere in the document is equally empty.
    [Test]
    public void BuildsAProofForASlotNamingOnlyNonexistentRuleIds()
    {
        XDocument grammar = XDocument.Parse(SlotNamingOnlyNonexistentRuleGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = InactiveMemberProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Check, Does.Contain("s2"));
        Assert.That(proof.Check, Does.Contain("ghost"));
    }

    // Core rejection scenario: both members active must never license this proof kind.
    [Test]
    public void RefusesWhenBothMembersAreActive()
    {
        XDocument grammar = XDocument.Parse(BothMembersActiveGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(InactiveMemberProofs.TryBuild(grammar, item), Is.Null);
    }

    // Rejection: a Slot whose referenced rules are ALL active must never be certified as empty.
    [Test]
    public void RefusesWhenBothSlotsResolveToAnActiveRule()
    {
        XDocument grammar = XDocument.Parse(BothSlotsResolveToAnActiveRuleGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(InactiveMemberProofs.TryBuild(grammar, item), Is.Null);
    }

    // Rejection: a Slot with SOME active and SOME inactive rules can still contribute a morpheme, so it
    // must not be certified as empty even though one of its rules resolves to nothing.
    [Test]
    public void RefusesWhenASlotHasSomeActiveAndSomeInactiveRules()
    {
        XDocument grammar = XDocument.Parse(SlotWithSomeActiveAndSomeInactiveRulesGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(InactiveMemberProofs.TryBuild(grammar, item), Is.Null);
    }

    // Rejection: the empty-slot resolution is reachable only through CheckSlotPair for an
    // AffixTemplateSlots pair, never through CheckIdrefPair for a Stratum IDREFS pair.
    [Test]
    public void RefusesToApplyEmptySlotResolutionToNonSlotOrderingKinds()
    {
        XDocument grammar = XDocument.Parse(BothMembersActiveGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();
        Assert.That(item.Kind, Is.EqualTo(OrderingListKind.StratumMorphologicalRules));

        Assert.That(InactiveMemberProofs.TryBuild(grammar, item), Is.Null);
    }

    // Verify-goes-stale: a proof built while s2's only rule was inactive must be REJECTED once that
    // rule is reactivated in a mutated document.
    [Test]
    public void VerifyRejectsAnEmptySlotProofOnceItsRuleIsReactivated()
    {
        XDocument original = XDocument.Parse(EmptySlotOnlyRuleInactiveGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(original, "fx").Single();
        Proof proof = InactiveMemberProofs.TryBuild(original, item)!;

        XDocument reactivated = XDocument.Parse(EmptySlotRuleReactivatedGrammar);

        Assert.That(InactiveMemberProofs.Verify(reactivated, "fx", proof), Is.False);
    }

    // Guard: an unresolvable member must fail closed, never be treated as inactive by default.
    [Test]
    public void RefusesWhenAMemberCannotBeResolvedToAnyDeclaration()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language><Strata>
              <Stratum morphologicalRules="ghost1 ghost2"><Name>S</Name></Stratum>
            </Strata></Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(InactiveMemberProofs.TryBuild(grammar, item), Is.Null);
    }

    // The core rejection the task asks for: a proof built while m2 was inactive must be REJECTED once m2
    // is reactivated, because Verify recomputes rather than trusts.
    [Test]
    public void VerifyRejectsAProofOnceTheMemberIsReactivated()
    {
        XDocument original = XDocument.Parse(SecondMemberInactiveGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(original, "fx").Single();
        Proof proof = InactiveMemberProofs.TryBuild(original, item)!;

        XDocument reactivated = XDocument.Parse(BothMembersActiveGrammar);

        Assert.That(InactiveMemberProofs.Verify(reactivated, "fx", proof), Is.False);
    }

    [Test]
    public void VerifyRejectsAStaleProofWhoseItemNoLongerExists()
    {
        XDocument grammar = XDocument.Parse(SecondMemberInactiveGrammar);
        var stale = new Proof("ordering:fx/morphologicalRules/gone~alsoGone", InactiveMemberProofs.Kind, "stale");

        Assert.That(InactiveMemberProofs.Verify(grammar, "fx", stale), Is.False);
    }
}
