using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;
using RuleInteractionRow = SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage.RuleInteractionLedger.Row;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class OrderingGeneratorTests
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

    // A small, hand-built document exercising all three list kinds: one Stratum carrying a
    // phonologicalRules list of 3 and a morphologicalRules list of 2, and one AffixTemplate with 3
    // ordered Slot children. Not required to be DTD-valid (parsed via XDocument.Parse, never loaded
    // through the engine), only structurally representative of what the generator reads.
    private const string SmallHandBuiltGrammar = """
        <HermitCrabInput>
          <Language>
            <Strata>
              <Stratum phonologicalRules="p1 p2 p3" morphologicalRules="m1 m2">
                <Name>Only</Name>
                <AffixTemplates>
                  <AffixTemplate>
                    <Name>tmpl</Name>
                    <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
                    <Slot morphologicalRules="m2"><Name>s2</Name></Slot>
                    <Slot morphologicalRules="m1"><Name>s3</Name></Slot>
                  </AffixTemplate>
                </AffixTemplates>
              </Stratum>
            </Strata>
          </Language>
        </HermitCrabInput>
        """;

    [Test]
    public void EnumerateOrderedListsFindsAllThreeKindsOnASmallHandBuiltDocument()
    {
        XDocument grammar = XDocument.Parse(SmallHandBuiltGrammar);

        IReadOnlyList<OrderedList> lists = OrderingGenerator.EnumerateOrderedLists(grammar, "fx");

        Assert.That(lists, Has.Count.EqualTo(3));
        OrderedList phono = lists.Single(l => l.Kind == OrderingListKind.StratumPhonologicalRules);
        Assert.That(phono.Members, Is.EqualTo(new[] { "p1", "p2", "p3" }));
        OrderedList morph = lists.Single(l => l.Kind == OrderingListKind.StratumMorphologicalRules);
        Assert.That(morph.Members, Is.EqualTo(new[] { "m1", "m2" }));
        OrderedList slots = lists.Single(l => l.Kind == OrderingListKind.AffixTemplateSlots);
        Assert.That(slots.Members, Is.EqualTo(new[] { "s1", "s2", "s3" }));
    }

    [Test]
    public void EnumerateAdjacentPairsEmitsNMinusOneItemsPerListNeverAllPairsOrPermutations()
    {
        XDocument grammar = XDocument.Parse(SmallHandBuiltGrammar);

        IReadOnlyList<OrderingItem> items = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx");

        // 3-member list -> 2 adjacent pairs, twice (phonological + slots); 2-member list -> 1 pair.
        Assert.That(items, Has.Count.EqualTo(5));
        Assert.That(items.Select(i => (i.MemberA, i.MemberB)), Does.Contain(("p1", "p2")));
        Assert.That(items.Select(i => (i.MemberA, i.MemberB)), Does.Contain(("p2", "p3")));
        Assert.That(
            items.Select(i => (i.MemberA, i.MemberB)),
            Does.Not.Contain(("p1", "p3")),
            "non-adjacent pairs must never be emitted"
        );
        Assert.That(items.Select(i => (i.MemberA, i.MemberB)), Does.Contain(("m1", "m2")));
        Assert.That(items.Select(i => (i.MemberA, i.MemberB)), Does.Contain(("s1", "s2")));
        Assert.That(items.Select(i => (i.MemberA, i.MemberB)), Does.Contain(("s2", "s3")));
        Assert.That(
            items.Select(i => i.Id).Distinct().ToArray(),
            Has.Length.EqualTo(5),
            "every item id must be unique"
        );
    }

    [Test]
    public void EnumerationIsDeterministicAcrossRepeatedCalls()
    {
        XDocument grammar = XDocument.Parse(SmallHandBuiltGrammar);

        IReadOnlyList<OrderingItem> first = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx");
        IReadOnlyList<OrderingItem> second = OrderingGenerator.EnumerateAdjacentPairs(
            XDocument.Parse(SmallHandBuiltGrammar),
            "fx"
        );

        Assert.That(
            first,
            Is.EqualTo(second),
            "re-parsing and re-enumerating the identical document must produce identical items"
        );
    }

    [Test]
    public void SwappingAPhonologicalRulesPairProducesExactlyOneTranspositionAndLeavesTheOriginalUntouched()
    {
        XDocument grammar = XDocument.Parse(SmallHandBuiltGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.Kind == OrderingListKind.StratumPhonologicalRules && i.MemberA == "p2" && i.MemberB == "p3");

        OrderingSwap? swap = OrderingGenerator.Swap(grammar, item);

        Assert.That(swap, Is.Not.Null);
        string? swapped = (string?)swap!.Mutated.Descendants("Stratum").Single().Attribute("phonologicalRules");
        Assert.That(swapped, Is.EqualTo("p1 p3 p2"), "only the adjacent p2/p3 pair may move; p1 stays first");

        // The original document must be untouched.
        Assert.That(
            (string?)grammar.Descendants("Stratum").Single().Attribute("phonologicalRules"),
            Is.EqualTo("p1 p2 p3")
        );
    }

    [Test]
    public void SwappingASlotPairMovesOnlyThoseTwoSlotsAndLeavesTheOriginalUntouched()
    {
        XDocument grammar = XDocument.Parse(SmallHandBuiltGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.Kind == OrderingListKind.AffixTemplateSlots && i.MemberA == "s1" && i.MemberB == "s2");

        OrderingSwap? swap = OrderingGenerator.Swap(grammar, item);

        Assert.That(swap, Is.Not.Null);
        string?[] swappedNames = swap!
            .Mutated.Descendants("AffixTemplate")
            .Single()
            .Elements("Slot")
            .Select(s => (string?)s.Element("Name"))
            .ToArray();
        Assert.That(swappedNames, Is.EqualTo(new[] { "s2", "s1", "s3" }));

        string?[] originalNames = grammar
            .Descendants("AffixTemplate")
            .Single()
            .Elements("Slot")
            .Select(s => (string?)s.Element("Name"))
            .ToArray();
        Assert.That(originalNames, Is.EqualTo(new[] { "s1", "s2", "s3" }), "the original document must be untouched");
    }

    [Test]
    public void SwapReturnsNullWhenTheItemNoLongerMatchesTheDocument()
    {
        XDocument grammar = XDocument.Parse(SmallHandBuiltGrammar);
        OrderingItem stale = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.MemberA == "p1" && i.MemberB == "p2");
        XDocument changed = XDocument.Parse(SmallHandBuiltGrammar.Replace("p1 p2 p3", "p2 p1 p3"));

        Assert.That(OrderingGenerator.Swap(changed, stale), Is.Null);
    }

    [Test]
    public void DisjointDomainsIsUndeterminedForListKindsThisCheckDoesNotModel()
    {
        XDocument grammar = XDocument.Parse(SmallHandBuiltGrammar);
        IReadOnlyList<OrderingItem> items = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx");

        DisjointDomainsCheck morphResult = OrderingGenerator.CheckDisjointDomains(
            grammar,
            items.Single(i => i.Kind == OrderingListKind.StratumMorphologicalRules)
        );
        Assert.That(morphResult.Relation, Is.EqualTo(DomainRelation.Undetermined));

        DisjointDomainsCheck slotResult = OrderingGenerator.CheckDisjointDomains(
            grammar,
            items.First(i => i.Kind == OrderingListKind.AffixTemplateSlots)
        );
        Assert.That(slotResult.Relation, Is.EqualTo(DomainRelation.Undetermined));
    }

    [Test]
    public void DisjointDomainsIsUndeterminedForAMetathesisRuleMember()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language><PhonologicalRuleDefinitions>
              <MetathesisRule id="mt1" leftSwitch="l" rightSwitch="r"><Name>m</Name>
                <StructuralDescription><PhoneticTemplate><PhoneticSequence>
                  <SimpleContext id="l" naturalClass="nc1" /><SimpleContext id="r" naturalClass="nc1" />
                </PhoneticSequence></PhoneticTemplate></StructuralDescription>
              </MetathesisRule>
              <PhonologicalRule id="pr1"><Name>p</Name>
                <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="nc1" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><SimpleContext naturalClass="nc1" /></PhoneticSequence></PhoneticOutput>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
            </PhonologicalRuleDefinitions>
            <Strata><Stratum phonologicalRules="mt1 pr1"><Name>Only</Name></Stratum></Strata>
            </Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        DisjointDomainsCheck result = OrderingGenerator.CheckDisjointDomains(grammar, item);

        Assert.That(result.Relation, Is.EqualTo(DomainRelation.Undetermined));
        Assert.That(result.Reason, Does.Contain("MetathesisRule"));
    }

    [Test]
    public void DisjointDomainsIsUndeterminedForARawPhoneticShapeOutput()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language><PhonologicalRuleDefinitions>
              <PhonologicalRule id="pr1"><Name>p1</Name>
                <PhoneticInput><PhoneticSequence><Segment segment="cA" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><Segments><PhoneticShape>ab</PhoneticShape></Segments></PhoneticSequence></PhoneticOutput>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
              <PhonologicalRule id="pr2"><Name>p2</Name>
                <PhoneticInput><PhoneticSequence><Segment segment="cB" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><Segment segment="cA" /></PhoneticSequence></PhoneticOutput>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
            </PhonologicalRuleDefinitions>
            <Strata><Stratum phonologicalRules="pr1 pr2"><Name>Only</Name></Stratum></Strata>
            </Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        DisjointDomainsCheck result = OrderingGenerator.CheckDisjointDomains(grammar, item);

        Assert.That(result.Relation, Is.EqualTo(DomainRelation.Undetermined));
        Assert.That(result.Reason, Does.Contain("not a modeled phonetic-sequence construct"));
    }

    // A FeatureNaturalClass that overlaps a SegmentNaturalClass must be detected as Overlaps, never
    // Disjoint. cA is both segNc's sole member and the only segment satisfying featNc's feature
    // constraint.
    [Test]
    public void DisjointDomainsDetectsAFeatureNaturalClassOverlappingASegmentNaturalClassAsOverlaps()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language>
            <CharacterDefinitionTable id="tbl"><Name>Main</Name><SegmentDefinitions>
              <SegmentDefinition id="cA"><Representations><Representation>a</Representation></Representations>
                <FeatureValue feature="featHigh" symbolValues="hiPlus" /></SegmentDefinition>
              <SegmentDefinition id="cB"><Representations><Representation>b</Representation></Representations>
                <FeatureValue feature="featHigh" symbolValues="hiMinus" /></SegmentDefinition>
            </SegmentDefinitions></CharacterDefinitionTable>
            <NaturalClasses>
              <SegmentNaturalClass id="segNc"><Name>segOnly</Name><Segment segment="cA" /></SegmentNaturalClass>
              <FeatureNaturalClass id="featNc"><Name>featHighOnly</Name>
                <FeatureValue feature="featHigh" symbolValues="hiPlus" /></FeatureNaturalClass>
            </NaturalClasses>
            <PhonologicalRuleDefinitions>
              <PhonologicalRule id="pr1"><Name>p1</Name>
                <PhoneticInput><PhoneticSequence><Segment segment="cB" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><SimpleContext naturalClass="segNc" /></PhoneticSequence></PhoneticOutput>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
              <PhonologicalRule id="pr2"><Name>p2</Name>
                <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="featNc" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><Segment segment="cB" /></PhoneticSequence></PhoneticOutput>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
            </PhonologicalRuleDefinitions>
            <Strata><Stratum phonologicalRules="pr1 pr2"><Name>Only</Name></Stratum></Strata>
            </Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        DisjointDomainsCheck result = OrderingGenerator.CheckDisjointDomains(grammar, item);

        Assert.That(result.Relation, Is.EqualTo(DomainRelation.Overlaps), result.Reason);
        Assert.That(result.Reason, Does.Contain("cA"));
    }

    [Test]
    public void DisjointDomainsFindsTwoGenuinelyNonIntersectingSegmentNaturalClassesDisjoint()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language>
            <NaturalClasses>
              <SegmentNaturalClass id="ncA"><Name>aOnly</Name><Segment segment="cA" /></SegmentNaturalClass>
              <SegmentNaturalClass id="ncB"><Name>bOnly</Name><Segment segment="cB" /></SegmentNaturalClass>
            </NaturalClasses>
            <PhonologicalRuleDefinitions>
              <PhonologicalRule id="pr1"><Name>p1</Name>
                <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncA" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><SimpleContext naturalClass="ncA" /></PhoneticSequence></PhoneticOutput>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
              <PhonologicalRule id="pr2"><Name>p2</Name>
                <PhoneticInput><PhoneticSequence><SimpleContext naturalClass="ncB" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><SimpleContext naturalClass="ncB" /></PhoneticSequence></PhoneticOutput>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
            </PhonologicalRuleDefinitions>
            <Strata><Stratum phonologicalRules="pr1 pr2"><Name>Only</Name></Stratum></Strata>
            </Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        DisjointDomainsCheck result = OrderingGenerator.CheckDisjointDomains(grammar, item);

        Assert.That(result.Relation, Is.EqualTo(DomainRelation.Disjoint), result.Reason);
    }

    // FEEDING: pr1's output (cA) never appears in pr2's PhoneticInput (cB), but pr2 only fires inside a
    // RightEnvironment of ncA -- pr1 can create that environment. Must be Overlaps, never Disjoint.
    [Test]
    public void DisjointDomainsDetectsFeedingThroughALaterRulesEnvironmentTemplate()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language>
            <NaturalClasses>
              <SegmentNaturalClass id="ncA"><Name>aOnly</Name><Segment segment="cA" /></SegmentNaturalClass>
            </NaturalClasses>
            <PhonologicalRuleDefinitions>
              <PhonologicalRule id="pr1"><Name>p1</Name>
                <PhoneticInput><PhoneticSequence><Segment segment="cX" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><Segment segment="cA" /></PhoneticSequence></PhoneticOutput>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
              <PhonologicalRule id="pr2"><Name>p2</Name>
                <PhoneticInput><PhoneticSequence><Segment segment="cB" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><Segment segment="cC" /></PhoneticSequence></PhoneticOutput>
                  <Environment>
                    <RightEnvironment><PhoneticTemplate><PhoneticSequence><SimpleContext naturalClass="ncA" /></PhoneticSequence></PhoneticTemplate></RightEnvironment>
                  </Environment>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
            </PhonologicalRuleDefinitions>
            <Strata><Stratum phonologicalRules="pr1 pr2"><Name>Only</Name></Stratum></Strata>
            </Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        DisjointDomainsCheck result = OrderingGenerator.CheckDisjointDomains(grammar, item);

        Assert.That(result.Relation, Is.EqualTo(DomainRelation.Overlaps), result.Reason);
        Assert.That(result.Reason, Does.Contain("cA"));
    }

    // BLEEDING: pr1's output (cY) never appears anywhere pr2 looks, but pr1's own PhoneticInput (cA) is
    // exactly the segment pr2's LeftEnvironment requires -- pr1 consuming/altering it can destroy the
    // environment pr2 needs. Must be Overlaps: EFFECT includes the earlier rule's own input, not only
    // its output.
    [Test]
    public void DisjointDomainsDetectsBleedingThroughTheEarlierRulesOwnConsumedInput()
    {
        XDocument grammar = XDocument.Parse(
            """
            <HermitCrabInput><Language>
            <NaturalClasses>
              <SegmentNaturalClass id="ncA"><Name>aOnly</Name><Segment segment="cA" /></SegmentNaturalClass>
            </NaturalClasses>
            <PhonologicalRuleDefinitions>
              <PhonologicalRule id="pr1"><Name>p1</Name>
                <PhoneticInput><PhoneticSequence><Segment segment="cA" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><Segment segment="cY" /></PhoneticSequence></PhoneticOutput>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
              <PhonologicalRule id="pr2"><Name>p2</Name>
                <PhoneticInput><PhoneticSequence><Segment segment="cB" /></PhoneticSequence></PhoneticInput>
                <PhonologicalSubrules><PhonologicalSubrule>
                  <PhoneticOutput><PhoneticSequence><Segment segment="cC" /></PhoneticSequence></PhoneticOutput>
                  <Environment>
                    <LeftEnvironment><PhoneticTemplate><PhoneticSequence><SimpleContext naturalClass="ncA" /></PhoneticSequence></PhoneticTemplate></LeftEnvironment>
                  </Environment>
                </PhonologicalSubrule></PhonologicalSubrules>
              </PhonologicalRule>
            </PhonologicalRuleDefinitions>
            <Strata><Stratum phonologicalRules="pr1 pr2"><Name>Only</Name></Stratum></Strata>
            </Language></HermitCrabInput>
            """
        );
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        DisjointDomainsCheck result = OrderingGenerator.CheckDisjointDomains(grammar, item);

        Assert.That(result.Relation, Is.EqualTo(DomainRelation.Overlaps), result.Reason);
        Assert.That(result.Reason, Does.Contain("cA"));
    }

    // Real-fixture check: the prAlpha/prHighTrigger adjacent swap in edge-cases/feature-system-breadth.
    // prAlpha's output is ncV (vowels: cI, cA); prHighTrigger's input is ncS ({cS}); the two never
    // intersect.
    [Test]
    public void RealFixtureFeatureSystemBreadthPrAlphaPrHighTriggerPairIsDisjointAndIdMatchesTheHandBuiltPilot()
    {
        XDocument grammar = XDocument.Load(
            Path.Combine(RepositoryRoot(), "conformance", "edge-cases", "feature-system-breadth", "grammar.xml")
        );
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "edge-cases/feature-system-breadth")
            .Single(i => i.MemberA == "prAlpha" && i.MemberB == "prHighTrigger");

        Assert.That(
            item.Id,
            Is.EqualTo("ordering:edge-cases/feature-system-breadth/phonologicalRules/prAlpha~prHighTrigger"),
            "must match CoveragePilotCandidatesTests' hand-built id for the same pair"
        );

        DisjointDomainsCheck result = OrderingGenerator.CheckDisjointDomains(grammar, item);
        Assert.That(result.Relation, Is.EqualTo(DomainRelation.Disjoint), result.Reason);
    }

    // Regression for a confirmed unsoundness: the OLD check compared only prNasalAssimAlveolar's
    // PhoneticOutput ({cNlv}) against prObstruentDeletion's PhoneticInput ({cK,cS,cT}) and certified
    // Disjoint, but prObstruentDeletion only fires inside a LeftEnvironment of ncNasal, and
    // prNasalAssimAlveolar's output IS nasal -- classic feeding. Swapping the pair empirically changes
    // 'menulik' from ok::NPFX+TULIK|menulik to ok::- (StructuralProofFalsificationTests pins the engine
    // side; this pins the structural check that must now see the overlap).
    [Test]
    public void RealFixtureMprGatedExceptionNasalAssimAlveolarObstruentDeletionPairIsNotDisjoint()
    {
        XDocument grammar = XDocument.Load(
            Path.Combine(RepositoryRoot(), "conformance", "edge-cases", "mpr-gated-exception", "grammar.xml")
        );
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "edge-cases/mpr-gated-exception")
            .Single(i => i.MemberA == "prNasalAssimAlveolar" && i.MemberB == "prObstruentDeletion");

        DisjointDomainsCheck result = OrderingGenerator.CheckDisjointDomains(grammar, item);

        Assert.That(result.Relation, Is.Not.EqualTo(DomainRelation.Disjoint), result.Reason);
        Assert.That(result.Relation, Is.EqualTo(DomainRelation.Overlaps), result.Reason);
        Assert.That(
            result.Reason,
            Does.Contain("cNlv"),
            "the shared segment must be the nasal prNasalAssimAlveolar produces and prObstruentDeletion's environment requires"
        );
    }

    // Pins measured numbers across the real corpus: 32 lists with >= 2 members, 146 adjacent pairs
    // total.
    [Test]
    public void RealCorpusProducesTheDesignDocsMeasuredListAndPairCounts()
    {
        string root = RepositoryRoot();
        List<Fixture> fixtures = Fixture.DiscoverAll(Path.Combine(root, "conformance"));
        Assert.That(fixtures, Has.Count.EqualTo(36));

        int totalLists = 0;
        int totalPairs = 0;
        int disjoint = 0;
        int overlaps = 0;
        int undetermined = 0;
        foreach (Fixture fixture in fixtures)
        {
            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            totalLists += OrderingGenerator.EnumerateOrderedLists(grammar, fixture.Id).Count;
            foreach (OrderingItem item in OrderingGenerator.EnumerateAdjacentPairs(grammar, fixture.Id))
            {
                totalPairs++;
                switch (OrderingGenerator.CheckDisjointDomains(grammar, item).Relation)
                {
                    case DomainRelation.Disjoint:
                        disjoint++;
                        break;
                    case DomainRelation.Overlaps:
                        overlaps++;
                        break;
                    default:
                        undetermined++;
                        break;
                }
            }
        }

        TestContext.Out.WriteLine(
            $"lists={totalLists} pairs={totalPairs} disjoint={disjoint} overlaps={overlaps} undetermined={undetermined}"
        );
        Assert.That(totalLists, Is.EqualTo(33));
        Assert.That(totalPairs, Is.EqualTo(154));
        Assert.That(disjoint + overlaps + undetermined, Is.EqualTo(154));
    }

    // Structural-only census (no engine parsing, so it is safe to run while the corpus is being edited
    // concurrently): how many of the real corpus's adjacent-pair items each recomputed proof kind covers,
    // tried in the same priority order CoverageEvidencePipeline.BuildProofs uses. Deliberately does not
    // pin an exact count -- unlike the pair-count test above, the whole point of this run is to report
    // the current split, not to gate on a frozen number of it.
    [Test]
    public void RealCorpusProofKindCensus()
    {
        string root = RepositoryRoot();
        List<Fixture> fixtures = Fixture.DiscoverAll(Path.Combine(root, "conformance"));

        int totalPairs = 0;
        int disjointDomains = 0;
        int unorderedInvariant = 0;
        int inactiveMember = 0;
        int posDisjoint = 0;
        int templateMasked = 0;
        int stillOpen = 0;
        var openByKind = new Dictionary<OrderingListKind, int>();
        var totalByKind = new Dictionary<OrderingListKind, int>();
        var inactiveMemberByKind = new Dictionary<OrderingListKind, int>();

        foreach (Fixture fixture in fixtures)
        {
            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            foreach (OrderingItem item in OrderingGenerator.EnumerateAdjacentPairs(grammar, fixture.Id))
            {
                totalPairs++;
                totalByKind[item.Kind] = totalByKind.GetValueOrDefault(item.Kind) + 1;
                if (OrderingProofs.TryBuild(grammar, item) is not null)
                {
                    disjointDomains++;
                }
                else if (UnorderedInvariantProofs.TryBuild(grammar, item) is not null)
                {
                    unorderedInvariant++;
                }
                else if (InactiveMemberProofs.TryBuild(grammar, item) is not null)
                {
                    inactiveMember++;
                    inactiveMemberByKind[item.Kind] = inactiveMemberByKind.GetValueOrDefault(item.Kind) + 1;
                }
                else if (PosDisjointProofs.TryBuild(grammar, item) is not null)
                {
                    posDisjoint++;
                }
                else if (TemplateMaskedProofs.TryBuild(grammar, item) is not null)
                {
                    templateMasked++;
                }
                else
                {
                    stillOpen++;
                    openByKind[item.Kind] = openByKind.GetValueOrDefault(item.Kind) + 1;
                }
            }
        }

        TestContext.Out.WriteLine($"total pairs:          {totalPairs}");
        foreach (var kv in totalByKind.OrderByDescending(kv => kv.Value))
            TestContext.Out.WriteLine($"  total, {kv.Key}: {kv.Value}");
        TestContext.Out.WriteLine($"disjoint-domains:      {disjointDomains}");
        TestContext.Out.WriteLine($"unordered-invariant:   {unorderedInvariant}");
        TestContext.Out.WriteLine($"inactive-member:       {inactiveMember}");
        foreach (var kv in inactiveMemberByKind.OrderByDescending(kv => kv.Value))
            TestContext.Out.WriteLine($"  inactive-member, {kv.Key}: {kv.Value}");
        TestContext.Out.WriteLine($"pos-disjoint:          {posDisjoint}");
        TestContext.Out.WriteLine($"template-masked:       {templateMasked}");
        TestContext.Out.WriteLine($"still open (no proof): {stillOpen}");
        foreach (var kv in openByKind.OrderByDescending(kv => kv.Value))
            TestContext.Out.WriteLine($"  open, {kv.Key}: {kv.Value}");

        Assert.That(
            disjointDomains + unorderedInvariant + inactiveMember + posDisjoint + templateMasked + stillOpen,
            Is.EqualTo(totalPairs),
            "every pair must be classified into exactly one bucket -- no double counting, none dropped"
        );
    }

    // Same hand-built document as the adjacent-pairs tests above: 2 free morphological rules (m1, m2), 1
    // AffixTemplate (tmpl -- a whole unit, its 3 Slots are not exploded), 3 phonological rules (p1, p2, p3),
    // all in a single Stratum. Morphology-stage units {m1, m2, tmpl}: 3x3 = 9 same-stage pairs (both
    // directions, self included, since mrules/templates are mutually recursive). Phonology-stage units {p1,
    // p2, p3}: only forward-or-self pairs, since the phonological cascade is unconditionally linear --
    // 3+2+1 = 6. Cross-stage: 3 morphology units x 3 phonology units, morphology-first only = 9. No second
    // Stratum, so 0 cross-stratum pairs. Total 24.
    [Test]
    public void EnumerateStratumPairsAppliesStageAndDirectionRulesOnASmallHandBuiltDocument()
    {
        XDocument grammar = XDocument.Parse(SmallHandBuiltGrammar);

        IReadOnlyList<StratumInteractionPair> pairs = OrderingGenerator.EnumerateStratumPairs(grammar, "fx");

        Assert.That(pairs, Has.Count.EqualTo(24));
        Assert.That(
            pairs.Count(p => p.Kind == StratumPairKind.SameStage),
            Is.EqualTo(15),
            "9 morphology + 6 phonology"
        );
        Assert.That(pairs.Count(p => p.Kind == StratumPairKind.CrossStage), Is.EqualTo(9));
        Assert.That(pairs.Count(p => p.Kind == StratumPairKind.CrossStratum), Is.EqualTo(0), "only one Stratum exists");

        bool HasPair(StratumUnitKind kindA, string a, StratumUnitKind kindB, string b) =>
            pairs.Any(p => p.UnitA.Kind == kindA && p.UnitA.Label == a && p.UnitB.Kind == kindB && p.UnitB.Label == b);

        // Self-pairs are included.
        Assert.That(HasPair(StratumUnitKind.MorphologicalRule, "m1", StratumUnitKind.MorphologicalRule, "m1"), Is.True);
        Assert.That(HasPair(StratumUnitKind.PhonologicalRule, "p1", StratumUnitKind.PhonologicalRule, "p1"), Is.True);

        // A template can feed a free morphological rule and vice versa (mutual recursion) -- both directions exist.
        Assert.That(HasPair(StratumUnitKind.AffixTemplate, "tmpl", StratumUnitKind.MorphologicalRule, "m1"), Is.True);
        Assert.That(HasPair(StratumUnitKind.MorphologicalRule, "m1", StratumUnitKind.AffixTemplate, "tmpl"), Is.True);

        // Non-adjacent phonological pair p1~p3 is permitted forward; adjacent-pairs generator never emits it.
        Assert.That(HasPair(StratumUnitKind.PhonologicalRule, "p1", StratumUnitKind.PhonologicalRule, "p3"), Is.True);
        // A later-declared phonological rule can never feed an earlier one -- the cascade is strictly linear.
        Assert.That(HasPair(StratumUnitKind.PhonologicalRule, "p3", StratumUnitKind.PhonologicalRule, "p1"), Is.False);

        // Morphology always precedes phonology within one Stratum pass; never the reverse.
        Assert.That(HasPair(StratumUnitKind.MorphologicalRule, "m1", StratumUnitKind.PhonologicalRule, "p1"), Is.True);
        Assert.That(HasPair(StratumUnitKind.PhonologicalRule, "p1", StratumUnitKind.MorphologicalRule, "m1"), Is.False);
    }

    // Regression pin for the pipeline constraint the whole design rests on: synthesis runs a Stratum's
    // morphology stage before its phonology stage, so a phonology-first cross-stage pair must never be
    // emitted, on any fixture.
    [Test]
    public void NoCrossStagePairEverPutsPhonologyBeforeMorphology()
    {
        string root = RepositoryRoot();
        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(root, "conformance")))
        {
            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            foreach (StratumInteractionPair pair in OrderingGenerator.EnumerateStratumPairs(grammar, fixture.Id))
            {
                if (pair.Kind != StratumPairKind.CrossStage)
                    continue;
                Assert.That(pair.UnitA.Kind, Is.Not.EqualTo(StratumUnitKind.PhonologicalRule), pair.Id);
                Assert.That(pair.UnitB.Kind, Is.EqualTo(StratumUnitKind.PhonologicalRule), pair.Id);
            }
        }
    }

    // A rule's own EFFECT always includes its own consumed input, and its own SENSITIVE set always
    // includes that same input, so a self-pair on a rule with a non-empty PhoneticInput must always
    // Overlap -- self-feeding is a real, common case (multipleApplicationOrder defaults every
    // PhonologicalRule to iterative), never a false Disjoint.
    [Test]
    public void CheckStratumPairDisjointDomainsFindsAPhonologicalRuleSelfPairOverlapping()
    {
        XDocument grammar = XDocument.Load(
            Path.Combine(RepositoryRoot(), "conformance", "edge-cases", "feature-system-breadth", "grammar.xml")
        );
        StratumInteractionPair selfPair = OrderingGenerator
            .EnumerateStratumPairs(grammar, "edge-cases/feature-system-breadth")
            .Single(p =>
                p.Kind == StratumPairKind.SameStage
                && p.UnitA.Kind == StratumUnitKind.PhonologicalRule
                && p.UnitA.Label == "prAlpha"
                && p.UnitB.Label == "prAlpha"
            );

        DisjointDomainsCheck result = OrderingGenerator.CheckStratumPairDisjointDomains(grammar, selfPair);

        Assert.That(result.Relation, Is.EqualTo(DomainRelation.Overlaps), result.Reason);
    }

    // Cross-check against the adjacent-pairs generator's own pinned fixture case: the SAME two rules,
    // reached through EnumerateStratumPairs instead of EnumerateAdjacentPairs, must agree.
    [Test]
    public void CheckStratumPairDisjointDomainsAgreesWithAdjacentPairsOnTheSameRealFixturePair()
    {
        XDocument grammar = XDocument.Load(
            Path.Combine(RepositoryRoot(), "conformance", "edge-cases", "feature-system-breadth", "grammar.xml")
        );
        StratumInteractionPair pair = OrderingGenerator
            .EnumerateStratumPairs(grammar, "edge-cases/feature-system-breadth")
            .Single(p =>
                p.Kind == StratumPairKind.SameStage && p.UnitA.Label == "prAlpha" && p.UnitB.Label == "prHighTrigger"
            );

        DisjointDomainsCheck result = OrderingGenerator.CheckStratumPairDisjointDomains(grammar, pair);

        Assert.That(result.Relation, Is.EqualTo(DomainRelation.Disjoint), result.Reason);
    }

    // Pins what this generator emits over the real corpus: 1,305 rows drawn from the 36 of 43 declared
    // Strata that contribute any pair. The arithmetic, from the corpus itself: summing n^2 over each
    // stratum's unit inventory gives 1,480 unconstrained ordered pairs; the ordering constraint removes
    // 203 of them (80 backward same-stage phonological pairs, forbidden because SynthesisStratumRule
    // builds _prulesRule as an unconditional LinearRuleCascade, plus 123 reverse cross-stage pairs),
    // leaving 1,277 within-stratum; 28 cross-stratum rows bring it to 1,305.
    //
    // Two cautions, because this number is easy to over-read. The design docs quote 1,465 and 1,342 for
    // the same quantities; those are a cruder estimate over a different unit inventory, not this
    // computation, and 1,480 is what the corpus actually yields. And the ordering constraint removes only
    // 13.7% -- the morphology block is the full n^2 in both directions, because mrules and AffixTemplates
    // recurse mutually. This is a corpus statistic, not a bound on what the engine permits.
    [Test]
    public void RealCorpusProducesThePipelinePermittedStratumPairCountsAndDomainRelationBreakdown()
    {
        string root = RepositoryRoot();
        List<Fixture> fixtures = Fixture.DiscoverAll(Path.Combine(root, "conformance"));
        Assert.That(fixtures, Has.Count.EqualTo(36));

        List<RuleInteractionRow> rows = fixtures
            .SelectMany(fixture => RuleInteractionLedger.Compute(XDocument.Load(fixture.GrammarPath), fixture.Id))
            .ToList();

        TestContext.Out.WriteLine($"total={rows.Count}");
        foreach (var kv in rows.GroupBy(r => r.PairKind))
            TestContext.Out.WriteLine($"  {kv.Key}: {kv.Count()}");
        foreach (var kv in rows.GroupBy(r => r.Relation))
            TestContext.Out.WriteLine($"  {kv.Key}: {kv.Count()}");

        // 1461 -> 1464 with edge-cases/cross-table-root-respelling: one Final-stratum morphological rule against
        // the corpus-wide pipeline yields three SameStage pairs (two Overlaps, one Undetermined).
        Assert.That(rows, Has.Count.EqualTo(1464));
        Assert.That(rows.Count(r => r.PairKind == StratumPairKind.SameStage), Is.EqualTo(1270));
        Assert.That(rows.Count(r => r.PairKind == StratumPairKind.CrossStage), Is.EqualTo(166));
        Assert.That(rows.Count(r => r.PairKind == StratumPairKind.CrossStratum), Is.EqualTo(28));
        Assert.That(rows.Count(r => r.Relation == DomainRelation.Disjoint), Is.EqualTo(30));
        Assert.That(rows.Count(r => r.Relation == DomainRelation.Overlaps), Is.EqualTo(65));
        Assert.That(rows.Count(r => r.Relation == DomainRelation.Undetermined), Is.EqualTo(1369));
    }

    // Mirrors ConformanceFixtureGateTests.CheckedInCoverageTablesAreUpToDate: regenerate the ledger from the
    // real corpus and require the checked-in file to match byte for byte, so an enumerator change or a
    // fixture edit that shifts the denominator is caught as a diff instead of silently going stale.
    [Test]
    public void CheckedInRuleInteractionLedgerIsUpToDate()
    {
        string root = RepositoryRoot();
        List<RuleInteractionRow> rows = Fixture
            .DiscoverAll(Path.Combine(root, "conformance"))
            .SelectMany(fixture => RuleInteractionLedger.Compute(XDocument.Load(fixture.GrammarPath), fixture.Id))
            .ToList();

        string fresh = RuleInteractionLedger.ToText(rows);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, RuleInteractionLedger.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            fresh.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-rule-interaction-pairs --repository-root ."
        );
    }
}
