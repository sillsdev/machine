using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class PosDisjointProofsTests
{
    private const string DisjointNoBridgeGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="mA mB"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA" requiredPartsOfSpeech="posX"><Name>rA</Name></MorphologicalRule>
              <MorphologicalRule id="mB" requiredPartsOfSpeech="posY"><Name>rB</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string DisjointSelfPreservingOutputsGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="mA mB"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA" requiredPartsOfSpeech="posX" outputPartOfSpeech="posX"><Name>rA</Name></MorphologicalRule>
              <MorphologicalRule id="mB" requiredPartsOfSpeech="posY" outputPartOfSpeech="posY"><Name>rB</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string ComparedRuleCrossesPosBoundaryGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="mA mB"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA" requiredPartsOfSpeech="posX" outputPartOfSpeech="posY"><Name>rA</Name></MorphologicalRule>
              <MorphologicalRule id="mB" requiredPartsOfSpeech="posY" outputPartOfSpeech="posY"><Name>rB</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string OverlappingPosGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="mA mB"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA" requiredPartsOfSpeech="posX posY"><Name>rA</Name></MorphologicalRule>
              <MorphologicalRule id="mB" requiredPartsOfSpeech="posY"><Name>rB</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    // mC bridges posX -> posY: a root at posX can pass through mC and become eligible for mB.
    private const string BridgingRuleGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="mA mB mC"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA" requiredPartsOfSpeech="posX"><Name>rA</Name></MorphologicalRule>
              <MorphologicalRule id="mB" requiredPartsOfSpeech="posY"><Name>rB</Name></MorphologicalRule>
              <MorphologicalRule id="mC" requiredPartsOfSpeech="posX" outputPartOfSpeech="posY"><Name>rC</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string ThirdPartySelfPreservingRuleGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="mA mB mC"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA" requiredPartsOfSpeech="posX" outputPartOfSpeech="posX"><Name>rA</Name></MorphologicalRule>
              <MorphologicalRule id="mB" requiredPartsOfSpeech="posY" outputPartOfSpeech="posY"><Name>rB</Name></MorphologicalRule>
              <MorphologicalRule id="mC" requiredPartsOfSpeech="posX" outputPartOfSpeech="posX"><Name>rC</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string NoRestrictionGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="mA mB"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA"><Name>rA</Name></MorphologicalRule>
              <MorphologicalRule id="mB" requiredPartsOfSpeech="posY"><Name>rB</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string RealizationalRuleGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="mA mB"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA" requiredPartsOfSpeech="posX"><Name>rA</Name></MorphologicalRule>
              <RealizationalRule id="mB"><Name>rB</Name></RealizationalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    // PhonologicalRuleDefinitions is a sibling of Strata (the DTD's Language content model), never
    // nested inside Stratum the way MorphologicalRuleDefinitions is -- matching the real corpus shape.
    private const string PhonologicalDisjointNoBridgeGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prA"><Name>rA</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule requiredPartsOfSpeech="posX"></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prB"><Name>rB</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule requiredPartsOfSpeech="posY"></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prA prB"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    private const string PhonologicalOverlappingPosGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prA"><Name>rA</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule requiredPartsOfSpeech="posX posY"></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prB"><Name>rB</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule requiredPartsOfSpeech="posY"></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prA prB"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    // One PhonologicalSubrule on prA has no requiredPartsOfSpeech at all, so prA fires regardless of
    // POS even though its OTHER subrule is restricted to posX -- the whole rule is unrestricted.
    private const string PhonologicalUnrestrictedSubruleGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prA"><Name>rA</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule requiredPartsOfSpeech="posX"></PhonologicalSubrule>
                <PhonologicalSubrule></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prB"><Name>rB</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule requiredPartsOfSpeech="posY"></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prA prB"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    // prA's only subrule is isActive="no", so it never fires -- neither restricted nor unrestricted,
    // just unresolvable, and must fail closed to Undetermined rather than either extreme.
    private const string PhonologicalNoActiveSubruleGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prA"><Name>rA</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule isActive="no" requiredPartsOfSpeech="posX"></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prB"><Name>rB</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule requiredPartsOfSpeech="posY"></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prA prB"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    // mC bridges posX -> posY via a MorphologicalRule, exactly as the morphological-pair bridging test
    // does -- the bridge channel is document-wide, not scoped to phonological rules.
    private const string PhonologicalBridgingRuleGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prA"><Name>rA</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule requiredPartsOfSpeech="posX"></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prB"><Name>rB</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule requiredPartsOfSpeech="posY"></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata>
            <Stratum phonologicalRules="prA prB" morphologicalRules="mC"><Name>S</Name>
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mC" requiredPartsOfSpeech="posX" outputPartOfSpeech="posY"><Name>rC</Name></MorphologicalRule>
              </MorphologicalRuleDefinitions>
            </Stratum>
          </Strata>
        </Language></HermitCrabInput>
        """;

    private const string MetathesisRuleGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prA"><Name>rA</Name>
              <PhonologicalSubrules>
                <PhonologicalSubrule requiredPartsOfSpeech="posX"></PhonologicalSubrule>
              </PhonologicalSubrules>
            </PhonologicalRule>
            <MetathesisRule id="mrA"><Name>rMeta</Name></MetathesisRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prA mrA"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    [Test]
    public void BuildsAProofForDisjointRequiredPartsOfSpeechWithNoBridge()
    {
        XDocument grammar = XDocument.Parse(DisjointNoBridgeGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = PosDisjointProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Kind, Is.EqualTo(PosDisjointProofs.Kind));
        Assert.That(PosDisjointProofs.Verify(grammar, "fx", proof), Is.True);
    }

    [Test]
    public void BuildsAProofWhenEachComparedRuleOnlyPreservesItsOwnPartOfSpeech()
    {
        XDocument grammar = XDocument.Parse(DisjointSelfPreservingOutputsGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = PosDisjointProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(PosDisjointProofs.Verify(grammar, "fx", proof!), Is.True);
    }

    [Test]
    public void RefusesWhenAComparedRuleOutputsTheOtherRulesRequiredPartOfSpeech()
    {
        XDocument grammar = XDocument.Parse(ComparedRuleCrossesPosBoundaryGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(PosDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // Core rejection: overlapping requiredPartsOfSpeech must never license this proof kind.
    [Test]
    public void RefusesWhenRequiredPartsOfSpeechOverlap()
    {
        XDocument grammar = XDocument.Parse(OverlappingPosGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(PosDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // The dangerous case named explicitly in the task: a bridging rule exists, so disjointness must
    // NEVER be claimed even though mA and mB's own requiredPartsOfSpeech never intersect.
    [Test]
    public void RefusesWhenABridgingRuleExists()
    {
        XDocument grammar = XDocument.Parse(BridgingRuleGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.MemberA == "mA" && i.MemberB == "mB");

        Assert.That(PosDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    [Test]
    public void BuildsAProofWhenAThirdPartyRuleOnlyPreservesOneDisjointSide()
    {
        XDocument grammar = XDocument.Parse(ThirdPartySelfPreservingRuleGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.MemberA == "mA" && i.MemberB == "mB");

        Proof? proof = PosDisjointProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(PosDisjointProofs.Verify(grammar, "fx", proof!), Is.True);
    }

    // Guard: a rule with no requiredPartsOfSpeech applies regardless of POS, so it can never be proven
    // disjoint from anything.
    [Test]
    public void RefusesWhenEitherRuleDeclaresNoPosRestriction()
    {
        XDocument grammar = XDocument.Parse(NoRestrictionGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(PosDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: RealizationalRule has no requiredPartsOfSpeech attribute at all and is not modeled.
    [Test]
    public void RefusesAPairInvolvingARealizationalRule()
    {
        XDocument grammar = XDocument.Parse(RealizationalRuleGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(PosDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // Phonological pairs: two PhonologicalRule elements with disjoint requiredPartsOfSpeech across
    // their (active) PhonologicalSubrule children, no bridge.
    [Test]
    public void BuildsAProofForDisjointPhonologicalSubrulePosWithNoBridge()
    {
        XDocument grammar = XDocument.Parse(PhonologicalDisjointNoBridgeGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = PosDisjointProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Kind, Is.EqualTo(PosDisjointProofs.Kind));
        Assert.That(PosDisjointProofs.Verify(grammar, "fx", proof), Is.True);
    }

    [Test]
    public void RefusesWhenPhonologicalSubruleRequiredPartsOfSpeechOverlap()
    {
        XDocument grammar = XDocument.Parse(PhonologicalOverlappingPosGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(PosDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // The exact inversion the task calls out: an unrestricted subrule must never read as "disjoint
    // from everything" -- it must refuse the proof, the same as the morphological no-restriction case.
    [Test]
    public void RefusesWhenAPhonologicalRuleHasAnUnrestrictedSubrule()
    {
        XDocument grammar = XDocument.Parse(PhonologicalUnrestrictedSubruleGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(PosDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: a rule whose only subrule is inactive has no active POS gate at all -- unresolvable, not
    // "unrestricted" and not "disjoint".
    [Test]
    public void RefusesWhenAPhonologicalRuleHasNoActiveSubrule()
    {
        XDocument grammar = XDocument.Parse(PhonologicalNoActiveSubruleGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(PosDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // The bridging channel is document-wide: a MorphologicalRule bridging posX -> posY must block the
    // proof for a PhonologicalRule/PhonologicalRule pair exactly as it does for a morphological pair.
    [Test]
    public void RefusesWhenABridgingRuleExistsForAPhonologicalPair()
    {
        XDocument grammar = XDocument.Parse(PhonologicalBridgingRuleGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.MemberA == "prA" && i.MemberB == "prB");

        Assert.That(PosDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: MetathesisRule has no requiredPartsOfSpeech at all and is not modeled.
    [Test]
    public void RefusesAPairInvolvingAMetathesisRule()
    {
        XDocument grammar = XDocument.Parse(MetathesisRuleGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(PosDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // The core rejection the task asks for: a proof built with no bridge must be REJECTED once a
    // bridging rule is added, because Verify recomputes rather than trusts.
    [Test]
    public void VerifyRejectsAProofOnceABridgingRuleIsAdded()
    {
        XDocument original = XDocument.Parse(DisjointNoBridgeGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(original, "fx").Single();
        Proof proof = PosDisjointProofs.TryBuild(original, item)!;

        XDocument withBridge = XDocument.Parse(BridgingRuleGrammar);

        Assert.That(PosDisjointProofs.Verify(withBridge, "fx", proof), Is.False);
    }

    [Test]
    public void VerifyRejectsAStaleProofWhoseItemNoLongerExists()
    {
        XDocument grammar = XDocument.Parse(DisjointNoBridgeGrammar);
        var stale = new Proof("ordering:fx/morphologicalRules/gone~alsoGone", PosDisjointProofs.Kind, "stale");

        Assert.That(PosDisjointProofs.Verify(grammar, "fx", stale), Is.False);
    }
}
