using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class UnorderedInvariantProofsTests
{
    private const string UnorderedStratumGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum phonologicalRules="p1 p2" morphologicalRules="m1 m2" morphologicalRuleOrder="unordered">
            <Name>S</Name>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string LinearStratumGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="m1 m2" morphologicalRuleOrder="linear"><Name>S</Name></Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string DefaultOrderStratumGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum morphologicalRules="m1 m2"><Name>S</Name></Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    [Test]
    public void BuildsAProofForAMorphologicalRulesPairOnAnUnorderedStratum()
    {
        XDocument grammar = XDocument.Parse(UnorderedStratumGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.Kind == OrderingListKind.StratumMorphologicalRules);

        Proof? proof = UnorderedInvariantProofs.TryBuild(grammar, item);

        Assert.That(proof, Is.Not.Null);
        Assert.That(proof!.Kind, Is.EqualTo(UnorderedInvariantProofs.Kind));
        Assert.That(proof.Check, Does.Contain("unordered"));
        Assert.That(UnorderedInvariantProofs.Verify(grammar, "fx", proof), Is.True);
    }

    // Guard: the precondition (unordered) does not hold for a phonologicalRules pair on the SAME
    // unordered stratum -- StratumPhonologicalRules is compiled as an unconditional LinearRuleCascade
    // regardless of morphologicalRuleOrder, so this kind must never license a proof for it.
    [Test]
    public void RefusesAPhonologicalRulesPairEvenOnAnUnorderedStratum()
    {
        XDocument grammar = XDocument.Parse(UnorderedStratumGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, "fx")
            .Single(i => i.Kind == OrderingListKind.StratumPhonologicalRules);

        Assert.That(UnorderedInvariantProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: a linear stratum must never license this proof kind -- linear order is exactly the case
    // where adjacent position DOES matter.
    [Test]
    public void RefusesAMorphologicalRulesPairOnALinearStratum()
    {
        XDocument grammar = XDocument.Parse(LinearStratumGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(UnorderedInvariantProofs.TryBuild(grammar, item), Is.Null);
    }

    // Guard: an absent morphologicalRuleOrder resolves to the DTD default "linear", never treated as unordered.
    [Test]
    public void RefusesAMorphologicalRulesPairWhenOrderIsAbsent()
    {
        XDocument grammar = XDocument.Parse(DefaultOrderStratumGrammar);
        OrderingItem item = OrderingGenerator.EnumerateAdjacentPairs(grammar, "fx").Single();

        Assert.That(UnorderedInvariantProofs.TryBuild(grammar, item), Is.Null);
    }

    // The core rejection scenario the task asks for: a proof built while the stratum was unordered must
    // be REJECTED once the stratum is flipped to linear, because Verify recomputes rather than trusts.
    [Test]
    public void VerifyRejectsAProofOnceTheStratumIsFlippedToLinear()
    {
        XDocument original = XDocument.Parse(UnorderedStratumGrammar);
        OrderingItem item = OrderingGenerator
            .EnumerateAdjacentPairs(original, "fx")
            .Single(i => i.Kind == OrderingListKind.StratumMorphologicalRules);
        Proof proof = UnorderedInvariantProofs.TryBuild(original, item)!;

        XDocument flipped = XDocument.Parse(UnorderedStratumGrammar.Replace("unordered", "linear"));

        Assert.That(UnorderedInvariantProofs.Verify(flipped, "fx", proof), Is.False);
    }

    [Test]
    public void VerifyRejectsAStaleProofWhoseItemNoLongerExists()
    {
        XDocument grammar = XDocument.Parse(UnorderedStratumGrammar);
        var stale = new Proof("ordering:fx/morphologicalRules/gone~alsoGone", UnorderedInvariantProofs.Kind, "stale");

        Assert.That(UnorderedInvariantProofs.Verify(grammar, "fx", stale), Is.False);
    }
}
