using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class CoverageCompletenessGateTests
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

    private static CoverageItem Item(string id, CoverageItemKind kind = CoverageItemKind.Surface) =>
        new(id, kind, "dtd-element", "fx");

    private static Evidence WordEvidence(string itemId) =>
        new(itemId, "fx", "w", "ok::before", CounterexampleKind.Word, "ok::after", "removed 1 element", CounterfactualVerdict.Evidenced);

    [Test]
    public void AnItemWithNoEvidenceAndNoProofIsUnresolvedAndFailsCompleteness()
    {
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            Array.Empty<Evidence>(),
            Array.Empty<Proof>()
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Unresolved));
    }

    [Test]
    public void AnItemWithBothEvidenceAndAProofIsConflictingAndFailsCompleteness()
    {
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            new[] { WordEvidence("a") },
            new[] { new Proof("a", ImpossibilityProofs.NoConsumer, "no reference in the engine") }
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Conflicting));
    }

    [Test]
    public void AnItemWithEvidenceAndAnUnsupportedProofStillFailsCompleteness()
    {
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a"), Item("b") },
            new[] { WordEvidence("a") },
            new[] { new Proof("b", ImpossibilityProofs.NoConsumer, "checked-in prose claim") }
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single(i => i.ItemId == "b").Resolution, Is.EqualTo(CoverageResolution.Rejected));
    }

    [TestCase(ImpossibilityProofs.DtdDefault)]
    [TestCase(ImpossibilityProofs.NoConsumer)]
    [TestCase(ImpossibilityProofs.NotInSignature)]
    [TestCase(ImpossibilityProofs.BlockedByDefect)]
    [TestCase("unknown-proof-kind")]
    public void AnUnsupportedProofKindIsRejectedUntilItHasAMechanicalVerifier(string proofKind)
    {
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            Array.Empty<Evidence>(),
            new[] { new Proof("a", proofKind, "checked-in prose claim") }
        );

        Assert.That(report.IsComplete, Is.False);
        CoverageResolutionResult result = report.Items.Single();
        Assert.That(result.Resolution, Is.EqualTo(CoverageResolution.Rejected));
        Assert.That(result.Detail, Does.Contain("mechanical verifier"));
    }

    [TestCase("languages/suffixing-evidential-adjacency-chain", "person", "number", TemplateMaskedProofs.Kind)]
    [TestCase("edge-cases/feature-system-breadth", "prHighTrigger", "mtSwap", NeverFiresProofs.Kind)]
    [TestCase("edge-cases/mpr-gated-exception", "prNasalAssimBilabial", "prNasalAssimAlveolar", FeatureValueDisjointProofs.Kind)]
    public void ARecomputedStructuralProofKindIsAdmittedByTheGate(
        string fixtureId,
        string memberA,
        string memberB,
        string expectedKind
    )
    {
        string root = RepositoryRoot();
        Fixture fixture = Fixture.DiscoverAll(Path.Combine(root, "conformance")).Single(f => f.Id == fixtureId);
        XDocument grammar = XDocument.Load(fixture.GrammarPath);
        OrderingItem orderingItem = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, fixtureId)
            .Single(item => item.MemberA == memberA && item.MemberB == memberB);
        Proof? proof = expectedKind switch
        {
            var kind when kind == TemplateMaskedProofs.Kind => TemplateMaskedProofs.TryBuild(grammar, orderingItem),
            var kind when kind == NeverFiresProofs.Kind => NeverFiresProofs.TryBuild(grammar, orderingItem),
            var kind when kind == FeatureValueDisjointProofs.Kind => FeatureValueDisjointProofs.TryBuild(grammar, orderingItem),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedKind), expectedKind, "test proof kind")
        };
        Assert.That(proof, Is.Not.Null, $"{expectedKind} proof should build for the checked-in fixture");
        Assert.That(proof!.Kind, Is.EqualTo(expectedKind));

        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", fixtureId);
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { proof },
            loadGrammar: _ => grammar
        );

        Assert.That(report.IsComplete, Is.True, report.Items.Single().Detail);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Proven));
    }

    [Test]
    public void CountsAreReportedPerCounterexampleKindNeverBlended()
    {
        Evidence loadFailure = new("b", "fx", "w", "ok", CounterexampleKind.LoadFailure, "threw", "removed root", CounterfactualVerdict.RequiredToLoad);

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a"), Item("b") },
            new[] { WordEvidence("a"), loadFailure },
            Array.Empty<Proof>()
        );

        Assert.That(report.EvidencedCountsByCounterexampleKind[CounterexampleKind.Word], Is.EqualTo(1));
        Assert.That(report.EvidencedCountsByCounterexampleKind[CounterexampleKind.LoadFailure], Is.EqualTo(1));
    }

    [Test]
    public void AProofNamingAnItemNoLongerInTheInventoryIsOrphanedAndFailsCompleteness()
    {
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            new[] { WordEvidence("a") },
            new[] { new Proof("gone", ImpossibilityProofs.NoConsumer, "no reference in the engine") }
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.OrphanedProofItemIds, Is.EqualTo(new[] { "gone" }));
    }

    [Test]
    public void EvidenceNamingAnItemNoLongerInTheInventoryIsOrphanedAndFailsCompleteness()
    {
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            new[] { WordEvidence("a"), WordEvidence("gone") },
            Array.Empty<Proof>()
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.OrphanedEvidenceItemIds, Is.EqualTo(new[] { "gone" }));
    }

    [Test]
    public void EvidenceFromADifferentFixtureCannotResolveTheItem()
    {
        Evidence wrongFixture = WordEvidence("a") with { Fixture = "other-fixture" };

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            new[] { wrongFixture },
            Array.Empty<Proof>()
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Unresolved));
        Assert.That(report.Items.Single().Detail, Does.Contain("fixture"));
    }

    [Test]
    public void AnEvidenceRecordWithNoneCounterexampleKindDoesNotCountAsEvidence()
    {
        // Unobservable/Timeout results carry CounterexampleKind.None: not a counter-example, and
        // must not silently resolve the item without a proof.
        Evidence unobservable = new("a", "fx", null, null, CounterexampleKind.None, null, "none", CounterfactualVerdict.Unobservable);

        CompletenessReport report = CoverageCompletenessGate.Evaluate(new[] { Item("a") }, new[] { unobservable }, Array.Empty<Proof>());

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Unresolved));
    }

    [TestCase(CounterfactualVerdict.Timeout, CounterexampleKind.Word)]
    [TestCase(CounterfactualVerdict.Unobservable, CounterexampleKind.Word)]
    [TestCase(CounterfactualVerdict.Evidenced, CounterexampleKind.LoadFailure)]
    [TestCase(CounterfactualVerdict.RequiredToLoad, CounterexampleKind.Word)]
    public void AContradictoryVerdictAndCounterexampleKindDoesNotCountAsEvidence(
        CounterfactualVerdict verdict,
        CounterexampleKind counterexampleKind
    )
    {
        Evidence contradictory = new("a", "fx", "w", "before", counterexampleKind, "after", "mutation", verdict);

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            new[] { contradictory },
            Array.Empty<Proof>()
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Unresolved));
        Assert.That(report.EvidencedCountsByCounterexampleKind.Values.Sum(), Is.Zero);
    }

    [TestCase(CounterfactualVerdict.Evidenced)]
    [TestCase(CounterfactualVerdict.EvidencedJointly)]
    public void AWordCounterexampleCountsForEitherMechanicallyEvidencedVerdict(CounterfactualVerdict verdict)
    {
        Evidence evidence = new("a", "fx", "w", "before", CounterexampleKind.Word, "after", "mutation", verdict);

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            new[] { evidence },
            Array.Empty<Proof>()
        );

        Assert.That(report.IsComplete, Is.True);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Evidenced));
        Assert.That(report.EvidencedCountsByCounterexampleKind[CounterexampleKind.Word], Is.EqualTo(1));
    }

    [Test]
    public void AJointLoadFailureCounterexampleCountsAsEvidence()
    {
        Evidence evidence = new(
            "a",
            "fx",
            "w",
            "before",
            CounterexampleKind.LoadFailure,
            "FormatException: mutant rejected",
            "joint mutation",
            CounterfactualVerdict.EvidencedJointly
        );

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            new[] { evidence },
            Array.Empty<Proof>()
        );

        Assert.That(report.IsComplete, Is.True);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Evidenced));
        Assert.That(report.EvidencedCountsByCounterexampleKind[CounterexampleKind.LoadFailure], Is.EqualTo(1));
    }

    [TestCase("word")]
    [TestCase("example")]
    [TestCase("counterexample")]
    [TestCase("unchanged")]
    public void AnIncompleteCounterexamplePayloadDoesNotCountAsEvidence(string missing)
    {
        Evidence incomplete = new(
            "a",
            "fx",
            missing == "word" ? null : "w",
            missing == "example" ? null : "same",
            CounterexampleKind.Word,
            missing == "counterexample" ? null : missing == "unchanged" ? "same" : "different",
            "mutation",
            CounterfactualVerdict.Evidenced
        );

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            new[] { incomplete },
            Array.Empty<Proof>()
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Unresolved));
        Assert.That(report.EvidencedCountsByCounterexampleKind.Values.Sum(), Is.Zero);
    }

    [Test]
    public void AnInvalidEvidenceClaimCannotDisappearBehindAProof()
    {
        Evidence invalid = new(
            "a",
            "fx",
            null,
            null,
            CounterexampleKind.Word,
            null,
            "mutation",
            CounterfactualVerdict.Evidenced
        );

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { Item("a") },
            new[] { invalid },
            new[] { new Proof("a", ImpossibilityProofs.NoConsumer, "no reference in the engine") }
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.Not.EqualTo(CoverageResolution.Proven));
    }

    [Test]
    public void DuplicateEvidenceRowsAreRejectedBeforeInvalidRowsCanBeFilteredOut()
    {
        Evidence invalidDuplicate = WordEvidence("a") with { Verdict = CounterfactualVerdict.Timeout };

        Assert.Throws<ArgumentException>(() =>
            CoverageCompletenessGate.Evaluate(
                new[] { Item("a") },
                new[] { WordEvidence("a"), invalidDuplicate },
                Array.Empty<Proof>()
            )
        );
    }

    // Two PhonologicalRule elements whose input/output both name segment "cA", so CheckDisjointDomains recomputes Overlaps.
    private const string OverlappingPhonologicalRulesGrammar = """
        <HermitCrabInput><Language><PhonologicalRuleDefinitions>
          <PhonologicalRule id="pr1"><Name>p1</Name>
            <PhoneticInput><PhoneticSequence><Segment segment="cA" /></PhoneticSequence></PhoneticInput>
            <PhonologicalSubrules><PhonologicalSubrule>
              <PhoneticOutput><PhoneticSequence><Segment segment="cA" /></PhoneticSequence></PhoneticOutput>
            </PhonologicalSubrule></PhonologicalSubrules>
          </PhonologicalRule>
          <PhonologicalRule id="pr2"><Name>p2</Name>
            <PhoneticInput><PhoneticSequence><Segment segment="cA" /></PhoneticSequence></PhoneticInput>
            <PhonologicalSubrules><PhonologicalSubrule>
              <PhoneticOutput><PhoneticSequence><Segment segment="cA" /></PhoneticSequence></PhoneticOutput>
            </PhonologicalSubrule></PhonologicalSubrules>
          </PhonologicalRule>
        </PhonologicalRuleDefinitions>
        <Strata><Stratum phonologicalRules="pr1 pr2"><Name>Only</Name></Stratum></Strata>
        </Language></HermitCrabInput>
        """;

    // A Slot pair is never modeled by CheckDisjointDomains, so it is always Undetermined -- the common case, not a corner case.
    private const string UndeterminedSlotPairGrammar = """
        <HermitCrabInput><Language><Strata><Stratum><Name>Only</Name>
          <AffixTemplates><AffixTemplate><Name>tmpl</Name>
            <Slot morphologicalRules="m1"><Name>s1</Name></Slot>
            <Slot morphologicalRules="m2"><Name>s2</Name></Slot>
          </AffixTemplate></AffixTemplates>
        </Stratum></Strata></Language></HermitCrabInput>
        """;

    [Test]
    public void ADisjointDomainsProofForAPairThatRecomputesOverlapsIsRejectedNotProven()
    {
        XDocument grammar = XDocument.Parse(OverlappingPhonologicalRulesGrammar);
        OrderingItem orderingItem = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();
        Assert.That(
            OrderingGenerator.CheckDisjointDomains(grammar, orderingItem).Relation,
            Is.EqualTo(DomainRelation.Overlaps),
            "sanity: this pair must genuinely overlap for the rejection below to test the right thing"
        );

        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", "fx");
        var falseProof = new Proof(orderingItem.Id, OrderingProofs.Kind, "hand-waved claim of independence");

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { falseProof },
            loadGrammar: _ => grammar
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Rejected));
    }

    [Test]
    public void ADisjointDomainsProofForAPairThatRecomputesUndeterminedIsRejectedNotProven()
    {
        XDocument grammar = XDocument.Parse(UndeterminedSlotPairGrammar);
        OrderingItem orderingItem = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();
        Assert.That(
            OrderingGenerator.CheckDisjointDomains(grammar, orderingItem).Relation,
            Is.EqualTo(DomainRelation.Undetermined),
            "sanity: Slot pairs are never modeled by CheckDisjointDomains, so this must be Undetermined"
        );

        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", "fx");
        var falseProof = new Proof(orderingItem.Id, OrderingProofs.Kind, "hand-waved claim of independence");

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { falseProof },
            loadGrammar: _ => grammar
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Rejected));
    }

    [Test]
    public void ADisjointDomainsProofWithNoGrammarLoaderIsRejectedRatherThanTrusted()
    {
        XDocument grammar = XDocument.Parse(OverlappingPhonologicalRulesGrammar);
        OrderingItem orderingItem = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();
        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", "fx");
        var proof = new Proof(orderingItem.Id, OrderingProofs.Kind, "unverifiable without a grammar");

        CompletenessReport report = CoverageCompletenessGate.Evaluate(new[] { item }, Array.Empty<Evidence>(), new[] { proof });

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Rejected));
    }

    [Test]
    public void ADisjointDomainsProofThatRecomputesDisjointIsProven()
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
        OrderingItem orderingItem = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();
        Proof? proof = OrderingProofs.TryBuild(grammar, orderingItem);
        Assert.That(proof, Is.Not.Null);

        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", "fx");
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { proof! },
            loadGrammar: _ => grammar
        );

        Assert.That(report.IsComplete, Is.True);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Proven));
    }

    private const string UnorderedStratumGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="m1 m2" morphologicalRuleOrder="unordered"><Name>S</Name></Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    [Test]
    public void AnUnorderedInvariantProofThatRecomputesUnorderedIsProven()
    {
        XDocument grammar = XDocument.Parse(UnorderedStratumGrammar);
        OrderingItem orderingItem = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();
        Proof proof = UnorderedInvariantProofs.TryBuild(grammar, orderingItem)!;
        Assert.That(proof, Is.Not.Null, "sanity: this stratum is unordered");

        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", "fx");
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { proof },
            loadGrammar: _ => grammar
        );

        Assert.That(report.IsComplete, Is.True);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Proven));
    }

    // Guard: a hand-claimed unordered-invariant proof for a stratum that has since gone linear must be
    // REJECTED at gate time, not trusted as prose.
    [Test]
    public void AnUnorderedInvariantProofForANowLinearStratumIsRejectedNotProven()
    {
        XDocument grammar = XDocument.Parse(UnorderedStratumGrammar.Replace("unordered", "linear"));
        OrderingItem orderingItem = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();
        Assert.That(UnorderedInvariantProofs.TryBuild(grammar, orderingItem), Is.Null, "sanity: linear cannot license this kind");

        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", "fx");
        var falseProof = new Proof(orderingItem.Id, UnorderedInvariantProofs.Kind, "hand-waved claim of unordered invariance");

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { falseProof },
            loadGrammar: _ => grammar
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Rejected));
    }

    private const string InactiveSecondMemberGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="m1 m2"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="m1"><Name>r1</Name></MorphologicalRule>
              <MorphologicalRule id="m2" isActive="no"><Name>r2</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    [Test]
    public void AnInactiveMemberProofThatRecomputesInactiveIsProven()
    {
        XDocument grammar = XDocument.Parse(InactiveSecondMemberGrammar);
        OrderingItem orderingItem = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();
        Proof proof = InactiveMemberProofs.TryBuild(grammar, orderingItem)!;
        Assert.That(proof, Is.Not.Null, "sanity: m2 is inactive");

        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", "fx");
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { proof },
            loadGrammar: _ => grammar
        );

        Assert.That(report.IsComplete, Is.True);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Proven));
    }

    // Guard: a hand-claimed inactive-member proof where both members are active must be REJECTED.
    [Test]
    public void AnInactiveMemberProofWhereBothMembersAreActiveIsRejectedNotProven()
    {
        XDocument grammar = XDocument.Parse(InactiveSecondMemberGrammar.Replace(" isActive=\"no\"", ""));
        OrderingItem orderingItem = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();
        Assert.That(InactiveMemberProofs.TryBuild(grammar, orderingItem), Is.Null, "sanity: both members are active");

        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", "fx");
        var falseProof = new Proof(orderingItem.Id, InactiveMemberProofs.Kind, "hand-waved claim of inactivity");

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { falseProof },
            loadGrammar: _ => grammar
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Rejected));
    }

    private const string PosDisjointNoBridgeGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="mA mB"><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA" requiredPartsOfSpeech="posX"><Name>rA</Name></MorphologicalRule>
              <MorphologicalRule id="mB" requiredPartsOfSpeech="posY"><Name>rB</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    [Test]
    public void APosDisjointProofThatRecomputesDisjointIsProven()
    {
        XDocument grammar = XDocument.Parse(PosDisjointNoBridgeGrammar);
        OrderingItem orderingItem = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();
        Proof proof = PosDisjointProofs.TryBuild(grammar, orderingItem)!;
        Assert.That(proof, Is.Not.Null, "sanity: posX/posY never intersect and nothing bridges them");

        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", "fx");
        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { proof },
            loadGrammar: _ => grammar
        );

        Assert.That(report.IsComplete, Is.True);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Proven));
    }

    // Guard: a hand-claimed pos-disjoint proof must be REJECTED once a bridging rule exists in the
    // grammar it is re-verified against -- the exact danger the task calls out for this kind.
    [Test]
    public void APosDisjointProofIsRejectedNotProvenWhenABridgingRuleExists()
    {
        XDocument grammar = XDocument.Parse(
            PosDisjointNoBridgeGrammar.Replace(
                "</MorphologicalRuleDefinitions>",
                """
                <MorphologicalRule id="mC" requiredPartsOfSpeech="posX" outputPartOfSpeech="posY"><Name>rC</Name></MorphologicalRule>
                </MorphologicalRuleDefinitions>
                """
            )
        );
        OrderingItem orderingItem = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.MemberA == "mA" && i.MemberB == "mB");
        Assert.That(PosDisjointProofs.TryBuild(grammar, orderingItem), Is.Null, "sanity: mC bridges posX to posY");

        var item = new CoverageItem(orderingItem.Id, CoverageItemKind.Ordering, "adjacent-transposition", "fx");
        var falseProof = new Proof(orderingItem.Id, PosDisjointProofs.Kind, "hand-waved claim of pos-disjointness");

        CompletenessReport report = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { falseProof },
            loadGrammar: _ => grammar
        );

        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Rejected));
    }
}
