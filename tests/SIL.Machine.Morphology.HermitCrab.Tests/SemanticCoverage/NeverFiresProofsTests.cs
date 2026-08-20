using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class NeverFiresProofsTests
{
    // Mirrors edge-cases/feature-system-breadth's prHighTrigger~mtSwap shape: a FeatureNaturalClass
    // requiring featHigh=hiPlus AND featCons=consPlus, where no active segment declares both -- cI is
    // high but not a consonant, cT is a consonant but not high -- so prDead's LeftEnvironment can never
    // match and the rule can never fire.
    private const string DeadEnvironmentGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalFeatureSystem>
            <SymbolicFeature id="fHigh"><Name>high</Name><Symbols><Symbol id="hiPlus">+</Symbol><Symbol id="hiMinus">-</Symbol></Symbols></SymbolicFeature>
            <SymbolicFeature id="fCons"><Name>cons</Name><Symbols><Symbol id="consPlus">+</Symbol><Symbol id="consMinus">-</Symbol></Symbols></SymbolicFeature>
          </PhonologicalFeatureSystem>
          <CharacterDefinitionTable id="tbl"><Name>Main</Name><SegmentDefinitions>
            <SegmentDefinition id="cI"><Representations><Representation>i</Representation></Representations>
              <FeatureValue feature="fHigh" symbolValues="hiPlus" /><FeatureValue feature="fCons" symbolValues="consMinus" />
            </SegmentDefinition>
            <SegmentDefinition id="cT"><Representations><Representation>t</Representation></Representations>
              <FeatureValue feature="fHigh" symbolValues="hiMinus" /><FeatureValue feature="fCons" symbolValues="consPlus" />
            </SegmentDefinition>
          </SegmentDefinitions></CharacterDefinitionTable>
          <NaturalClasses>
            <SegmentNaturalClass id="ncS"><Name>sOnly</Name><Segment segment="cT" /></SegmentNaturalClass>
            <FeatureNaturalClass id="ncHighCons"><Name>highCons</Name>
              <FeatureValue feature="fHigh" symbolValues="hiPlus" /><FeatureValue feature="fCons" symbolValues="consPlus" />
            </FeatureNaturalClass>
          </NaturalClasses>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prDead"><Name>dead</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncS" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticOutput>
                <Environment><LeftEnvironment><PhoneticTemplate><PhoneticSequence><SimpleContext naturalClass="ncHighCons" /></PhoneticSequence></PhoneticTemplate></LeftEnvironment></Environment>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prOther"><Name>other</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncS" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticOutput>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prDead prOther"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    // cK now declares BOTH hiPlus and consPlus, so ncHighCons is non-empty -- prDead's environment can
    // match cK and the rule is no longer dead.
    private const string NonEmptyEnvironmentGrammar = """
        <HermitCrabInput><Language>
          <PhonologicalFeatureSystem>
            <SymbolicFeature id="fHigh"><Name>high</Name><Symbols><Symbol id="hiPlus">+</Symbol><Symbol id="hiMinus">-</Symbol></Symbols></SymbolicFeature>
            <SymbolicFeature id="fCons"><Name>cons</Name><Symbols><Symbol id="consPlus">+</Symbol><Symbol id="consMinus">-</Symbol></Symbols></SymbolicFeature>
          </PhonologicalFeatureSystem>
          <CharacterDefinitionTable id="tbl"><Name>Main</Name><SegmentDefinitions>
            <SegmentDefinition id="cI"><Representations><Representation>i</Representation></Representations>
              <FeatureValue feature="fHigh" symbolValues="hiPlus" /><FeatureValue feature="fCons" symbolValues="consMinus" />
            </SegmentDefinition>
            <SegmentDefinition id="cT"><Representations><Representation>t</Representation></Representations>
              <FeatureValue feature="fHigh" symbolValues="hiMinus" /><FeatureValue feature="fCons" symbolValues="consPlus" />
            </SegmentDefinition>
            <SegmentDefinition id="cK"><Representations><Representation>k</Representation></Representations>
              <FeatureValue feature="fHigh" symbolValues="hiPlus" /><FeatureValue feature="fCons" symbolValues="consPlus" />
            </SegmentDefinition>
          </SegmentDefinitions></CharacterDefinitionTable>
          <NaturalClasses>
            <SegmentNaturalClass id="ncS"><Name>sOnly</Name><Segment segment="cT" /></SegmentNaturalClass>
            <FeatureNaturalClass id="ncHighCons"><Name>highCons</Name>
              <FeatureValue feature="fHigh" symbolValues="hiPlus" /><FeatureValue feature="fCons" symbolValues="consPlus" />
            </FeatureNaturalClass>
          </NaturalClasses>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prDead"><Name>dead</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncS" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticOutput>
                <Environment><LeftEnvironment><PhoneticTemplate><PhoneticSequence><SimpleContext naturalClass="ncHighCons" /></PhoneticSequence></PhoneticTemplate></LeftEnvironment></Environment>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prOther"><Name>other</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncS" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticOutput>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prDead prOther"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    private const string EmptyInputClassGrammar = """
        <HermitCrabInput><Language>
          <CharacterDefinitionTable id="tbl"><Name>Main</Name><SegmentDefinitions>
            <SegmentDefinition id="cI"><Representations><Representation>i</Representation></Representations></SegmentDefinition>
          </SegmentDefinitions></CharacterDefinitionTable>
          <NaturalClasses>
            <!-- Declares no Segment children at all: an active class that is simply empty by
                 construction, distinct from an INACTIVE class (which would be a dangling-reference
                 load failure if referenced live, not a "resolves to empty" case). -->
            <SegmentNaturalClass id="ncEmpty"><Name>empty</Name></SegmentNaturalClass>
          </NaturalClasses>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prDead"><Name>dead</Name>
              <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncEmpty" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticOutput>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prOther"><Name>other</Name>
              <PhoneticInput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticOutput>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prDead prOther"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    private const string UnconditionedSubruleGrammar = """
        <HermitCrabInput><Language>
          <CharacterDefinitionTable id="tbl"><Name>Main</Name><SegmentDefinitions>
            <SegmentDefinition id="cI"><Representations><Representation>i</Representation></Representations></SegmentDefinition>
          </SegmentDefinitions></CharacterDefinitionTable>
          <PhonologicalRuleDefinitions>
            <PhonologicalRule id="prLive"><Name>live</Name>
              <PhoneticInput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticOutput>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
            <PhonologicalRule id="prOther"><Name>other</Name>
              <PhoneticInput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticInput>
              <PhonologicalSubrules><PhonologicalSubrule>
                <PhoneticOutput><PhoneticSequence><Segment segment="cI" /></PhoneticSequence></PhoneticOutput>
              </PhonologicalSubrule></PhonologicalSubrules>
            </PhonologicalRule>
          </PhonologicalRuleDefinitions>
          <Strata><Stratum phonologicalRules="prLive prOther"><Name>S</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    [Test]
    public void BuildsAProofWhenAnEnvironmentClassResolvesToZeroActiveSegments()
    {
        XDocument grammar = XDocument.Parse(DeadEnvironmentGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = NeverFiresProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Kind, Is.EqualTo(NeverFiresProofs.Kind));
        Assert.That(proof.Check, Does.Contain("prDead"));
        Assert.That(NeverFiresProofs.Verify(grammar, "fx", proof), Is.True);
    }

    [Test]
    public void BuildsAProofWhenTheSharedInputClassResolvesToZeroActiveSegments()
    {
        XDocument grammar = XDocument.Parse(EmptyInputClassGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Proof? proof = NeverFiresProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(NeverFiresProofs.Verify(grammar, "fx", proof!), Is.True);
    }

    // Core rejection the task calls out by name: a non-empty class must never license this proof kind.
    [Test]
    public void RefusesWhenTheEnvironmentClassIsNonEmpty()
    {
        XDocument grammar = XDocument.Parse(NonEmptyEnvironmentGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(NeverFiresProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: an active subrule with no Environment at all fires unconditionally, so it is never dead.
    [Test]
    public void RefusesWhenAnActiveSubruleHasNoEnvironmentAtAll()
    {
        XDocument grammar = XDocument.Parse(UnconditionedSubruleGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(NeverFiresProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: this kind is scoped to Stratum phonologicalRules pairs only.
    [Test]
    public void RefusesForAStratumMorphologicalRulesPair()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language><Strata>
              <Stratum morphologicalRules="m1 m2"><Name>S</Name>
                <MorphologicalRuleDefinitions>
                  <MorphologicalRule id="m1"><Name>r1</Name></MorphologicalRule>
                  <MorphologicalRule id="m2"><Name>r2</Name></MorphologicalRule>
                </MorphologicalRuleDefinitions>
              </Stratum>
            </Strata></Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(NeverFiresProofs.TryBuild(grammar, item), Is.Null);
    }

    // The core rejection the task asks for: a proof built while the environment class was empty must be
    // REJECTED once a segment is added that reactivates it (both hiPlus and consPlus now co-occur).
    [Test]
    public void VerifyRejectsAProofOnceAQualifyingSegmentIsAdded()
    {
        XDocument original = XDocument.Parse(DeadEnvironmentGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(original, "fx").Single();
        Proof proof = NeverFiresProofs.TryBuild(original, item)!;

        XDocument reactivated = XDocument.Parse(NonEmptyEnvironmentGrammar);

        Assert.That(NeverFiresProofs.Verify(reactivated, "fx", proof), Is.False);
    }

    [Test]
    public void VerifyRejectsAStaleProofWhoseItemNoLongerExists()
    {
        XDocument grammar = XDocument.Parse(DeadEnvironmentGrammar);
        var stale = new Proof("ordering:fx/phonologicalRules/gone~alsoGone", NeverFiresProofs.Kind, "stale");

        Assert.That(NeverFiresProofs.Verify(grammar, "fx", stale), Is.False);
    }
}
