using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class FeatureValueDisjointProofsTests
{
    // Mirrors edge-cases/mpr-gated-exception's prNasalAssimBilabial~prNasalAssimAlveolar shape: a shared
    // placeholder input (cN), each rule gated by a disjoint featPlace value, and outputs (cM/cAlv) that
    // belong to neither the other's environment nor its input class.
    private const string DisjointGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalFeatureSystem>
            <SymbolicFeature id="fPlace"><Name>place</Name><Symbols>
              <Symbol id="pOther">other</Symbol><Symbol id="pBilabial">bilabial</Symbol><Symbol id="pAlveolar">alveolar</Symbol>
            </Symbols></SymbolicFeature>
          </PhonologicalFeatureSystem>
          <CharacterDefinitionTable id="tbl"><Name>Main</Name><SegmentDefinitions>
            <SegmentDefinition id="cN"><Representations><Representation>N</Representation></Representations><FeatureValue feature="fPlace" symbolValues="pOther" /></SegmentDefinition>
            <SegmentDefinition id="cM"><Representations><Representation>m</Representation></Representations><FeatureValue feature="fPlace" symbolValues="pBilabial" /></SegmentDefinition>
            <SegmentDefinition id="cAlv"><Representations><Representation>n</Representation></Representations><FeatureValue feature="fPlace" symbolValues="pAlveolar" /></SegmentDefinition>
          </SegmentDefinitions></CharacterDefinitionTable>
          <NaturalClasses>
            <SegmentNaturalClass id="ncPlaceholder"><Name>placeholder</Name><Segment segment="cN" /></SegmentNaturalClass>
            <FeatureNaturalClass id="ncBilabial"><Name>bilabial</Name><FeatureValue feature="fPlace" symbolValues="pBilabial" /></FeatureNaturalClass>
            <FeatureNaturalClass id="ncAlveolar"><Name>alveolar</Name><FeatureValue feature="fPlace" symbolValues="pAlveolar" /></FeatureNaturalClass>
          </NaturalClasses>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prA"><Name>bilabialAssim</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncPlaceholder" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cM" /></PhoneticSequence></PhoneticOutput>
                <Environment><RightEnvironment><PhoneticTemplate><PhoneticSequence><SimpleContext naturalClass="ncBilabial" /></PhoneticSequence></PhoneticTemplate></RightEnvironment></Environment>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prB"><Name>alveolarAssim</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncPlaceholder" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cAlv" /></PhoneticSequence></PhoneticOutput>
                <Environment><RightEnvironment><PhoneticTemplate><PhoneticSequence><SimpleContext naturalClass="ncAlveolar" /></PhoneticSequence></PhoneticTemplate></RightEnvironment></Environment>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prA prB"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    // ncAlveolar now also admits pBilabial, so it shares cM with ncBilabial -- the environment classes
    // overlap and the pair must be refused.
    private const string OverlappingEnvironmentGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalFeatureSystem>
            <SymbolicFeature id="fPlace"><Name>place</Name><Symbols>
              <Symbol id="pOther">other</Symbol><Symbol id="pBilabial">bilabial</Symbol><Symbol id="pAlveolar">alveolar</Symbol>
            </Symbols></SymbolicFeature>
          </PhonologicalFeatureSystem>
          <CharacterDefinitionTable id="tbl"><Name>Main</Name><SegmentDefinitions>
            <SegmentDefinition id="cN"><Representations><Representation>N</Representation></Representations><FeatureValue feature="fPlace" symbolValues="pOther" /></SegmentDefinition>
            <SegmentDefinition id="cM"><Representations><Representation>m</Representation></Representations><FeatureValue feature="fPlace" symbolValues="pBilabial" /></SegmentDefinition>
            <SegmentDefinition id="cAlv"><Representations><Representation>n</Representation></Representations><FeatureValue feature="fPlace" symbolValues="pAlveolar" /></SegmentDefinition>
          </SegmentDefinitions></CharacterDefinitionTable>
          <NaturalClasses>
            <SegmentNaturalClass id="ncPlaceholder"><Name>placeholder</Name><Segment segment="cN" /></SegmentNaturalClass>
            <FeatureNaturalClass id="ncBilabial"><Name>bilabial</Name><FeatureValue feature="fPlace" symbolValues="pBilabial" /></FeatureNaturalClass>
            <FeatureNaturalClass id="ncAlveolar"><Name>alveolar</Name><FeatureValue feature="fPlace" symbolValues="pAlveolar pBilabial" /></FeatureNaturalClass>
          </NaturalClasses>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prA"><Name>bilabialAssim</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncPlaceholder" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cM" /></PhoneticSequence></PhoneticOutput>
                <Environment><RightEnvironment><PhoneticTemplate><PhoneticSequence><SimpleContext naturalClass="ncBilabial" /></PhoneticSequence></PhoneticTemplate></RightEnvironment></Environment>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prB"><Name>alveolarAssim</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncPlaceholder" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cAlv" /></PhoneticSequence></PhoneticOutput>
                <Environment><RightEnvironment><PhoneticTemplate><PhoneticSequence><SimpleContext naturalClass="ncAlveolar" /></PhoneticSequence></PhoneticTemplate></RightEnvironment></Environment>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prA prB"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    // prA's output is changed to cAlv, which IS a member of prB's own environment class ncAlveolar: firing
    // prA could create a brand-new site for prB, so the pair must be refused.
    private const string OutputInOtherEnvironmentGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalFeatureSystem>
            <SymbolicFeature id="fPlace"><Name>place</Name><Symbols>
              <Symbol id="pOther">other</Symbol><Symbol id="pBilabial">bilabial</Symbol><Symbol id="pAlveolar">alveolar</Symbol>
            </Symbols></SymbolicFeature>
          </PhonologicalFeatureSystem>
          <CharacterDefinitionTable id="tbl"><Name>Main</Name><SegmentDefinitions>
            <SegmentDefinition id="cN"><Representations><Representation>N</Representation></Representations><FeatureValue feature="fPlace" symbolValues="pOther" /></SegmentDefinition>
            <SegmentDefinition id="cM"><Representations><Representation>m</Representation></Representations><FeatureValue feature="fPlace" symbolValues="pBilabial" /></SegmentDefinition>
            <SegmentDefinition id="cAlv"><Representations><Representation>n</Representation></Representations><FeatureValue feature="fPlace" symbolValues="pAlveolar" /></SegmentDefinition>
          </SegmentDefinitions></CharacterDefinitionTable>
          <NaturalClasses>
            <SegmentNaturalClass id="ncPlaceholder"><Name>placeholder</Name><Segment segment="cN" /></SegmentNaturalClass>
            <FeatureNaturalClass id="ncBilabial"><Name>bilabial</Name><FeatureValue feature="fPlace" symbolValues="pBilabial" /></FeatureNaturalClass>
            <FeatureNaturalClass id="ncAlveolar"><Name>alveolar</Name><FeatureValue feature="fPlace" symbolValues="pAlveolar" /></FeatureNaturalClass>
          </NaturalClasses>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prA"><Name>bilabialAssim</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncPlaceholder" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cAlv" /></PhoneticSequence></PhoneticOutput>
                <Environment><RightEnvironment><PhoneticTemplate><PhoneticSequence><SimpleContext naturalClass="ncBilabial" /></PhoneticSequence></PhoneticTemplate></RightEnvironment></Environment>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prB"><Name>alveolarAssim</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncPlaceholder" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cAlv" /></PhoneticSequence></PhoneticOutput>
                <Environment><RightEnvironment><PhoneticTemplate><PhoneticSequence><SimpleContext naturalClass="ncAlveolar" /></PhoneticSequence></PhoneticTemplate></RightEnvironment></Environment>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prA prB"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    private const string NoEnvironmentGrammar = """
        <HermitCrabInput><Language>
          <CharacterDefinitionTable id="tbl"><Name>Main</Name><SegmentDefinitions>
            <SegmentDefinition id="cN"><Representations><Representation>N</Representation></Representations></SegmentDefinition>
            <SegmentDefinition id="cM"><Representations><Representation>m</Representation></Representations></SegmentDefinition>
          </SegmentDefinitions></CharacterDefinitionTable>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prA"><Name>a</Name>
              <PhoneticInput><PhoneticSequence><Segment segment="cN" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cM" /></PhoneticSequence></PhoneticOutput>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prB"><Name>b</Name>
              <PhoneticInput><PhoneticSequence><Segment segment="cN" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cM" /></PhoneticSequence></PhoneticOutput>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prA prB"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    [Test]
    public void BuildsAProofForDisjointPlaceEnvironmentsWithNoFeedingChannel()
    {
        XDocument grammar = XDocument.Parse(DisjointGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = FeatureValueDisjointProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Kind, Is.EqualTo(FeatureValueDisjointProofs.Kind));
        Assert.That(FeatureValueDisjointProofs.Verify(grammar, "fx", proof), Is.True);
    }

    // Core rejection: overlapping environment classes must never license this proof kind.
    [Test]
    public void RefusesWhenTheEnvironmentClassesOverlap()
    {
        XDocument grammar = XDocument.Parse(OverlappingEnvironmentGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(FeatureValueDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // Core rejection: an output that IS a member of the other rule's environment class must never license
    // this proof kind, even when the environment classes themselves are disjoint.
    [Test]
    public void RefusesWhenOneRulesOutputIsAMemberOfTheOthersEnvironmentClass()
    {
        XDocument grammar = XDocument.Parse(OutputInOtherEnvironmentGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(FeatureValueDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: a rule with no Environment at all fires unconditionally, so the mutual-exclusion argument
    // this kind depends on does not apply.
    [Test]
    public void RefusesWhenNeitherRuleDeclaresAnEnvironment()
    {
        XDocument grammar = XDocument.Parse(NoEnvironmentGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(FeatureValueDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: this kind is scoped to Stratum phonologicalRules pairs between two PhonologicalRule elements.
    [Test]
    public void RefusesAPairInvolvingAMetathesisRule()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language>
              <PhonologicalRuleDefinitions>
                <PhonologicalRule id="prA"><Name>a</Name>
                  <PhoneticInput><PhoneticSequence><Segment segment="cN" /></PhoneticSequence></PhoneticInput>
                  <PhonologicalSubrules><PhonologicalSubrule>
                    <PhoneticOutput><PhoneticSequence><Segment segment="cN" /></PhoneticSequence></PhoneticOutput>
                    <Environment><RightEnvironment><PhoneticTemplate><PhoneticSequence><Segment segment="cN" /></PhoneticSequence></PhoneticTemplate></RightEnvironment></Environment>
                  </PhonologicalSubrule></PhonologicalSubrules>
                </PhonologicalRule>
                <MetathesisRule id="mrA"><Name>m</Name></MetathesisRule>
              </PhonologicalRuleDefinitions>
              <Strata><Stratum phonologicalRules="prA mrA"><Name>S</Name></Stratum></Strata>
            </Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(FeatureValueDisjointProofs.TryBuild(grammar, item), Is.Null);
    }

    // The core rejection the task asks for: a proof built while the environment classes were disjoint must
    // be REJECTED once one class is widened to overlap the other.
    [Test]
    public void VerifyRejectsAProofOnceTheEnvironmentClassesAreWidenedToOverlap()
    {
        XDocument original = XDocument.Parse(DisjointGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(original, "fx").Single();
        Proof proof = FeatureValueDisjointProofs.TryBuild(original, item)!;

        XDocument widened = XDocument.Parse(OverlappingEnvironmentGrammar);

        Assert.That(FeatureValueDisjointProofs.Verify(widened, "fx", proof), Is.False);
    }

    // The second stale-proof scenario: a proof built while outputs stayed clear of each other's
    // environment must be REJECTED once one rule's output is changed to land inside the other's.
    [Test]
    public void VerifyRejectsAProofOnceAnOutputIsChangedToLandInTheOthersEnvironment()
    {
        XDocument original = XDocument.Parse(DisjointGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(original, "fx").Single();
        Proof proof = FeatureValueDisjointProofs.TryBuild(original, item)!;

        XDocument changedOutput = XDocument.Parse(OutputInOtherEnvironmentGrammar);

        Assert.That(FeatureValueDisjointProofs.Verify(changedOutput, "fx", proof), Is.False);
    }

    [Test]
    public void VerifyRejectsAStaleProofWhoseItemNoLongerExists()
    {
        XDocument grammar = XDocument.Parse(DisjointGrammar);
        var stale = new Proof("ordering:fx/phonologicalRules/gone~alsoGone", FeatureValueDisjointProofs.Kind, "stale");

        Assert.That(FeatureValueDisjointProofs.Verify(grammar, "fx", stale), Is.False);
    }
}
