using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

[TestFixture]
public class CopyAgreementPruneTests : HermitCrabTestBase
{
    [TestCase(false, 3)]
    [TestCase(true, 1)]
    public void FullCopyKeepsOnlyTheSplitWhoseCopiesAgree(bool pruneCopies, int expectedOutputs)
    {
        (string first, string second) = FindIncompatibleSegments();
        var anySegment = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        AffixProcessRule rule = CreateCopyRule(
            Pattern<Word, ShapeNode>.New("copy").Annotation(anySegment).OneOrMore.Value
        );
        var morpher = new Morpher(TraceManager, Language) { PruneDisagreeingCopies = pruneCopies };

        List<Word> outputs = new AnalysisAffixProcessRule(morpher, rule)
            .Apply(CreateInput(first + second + first + second))
            .ToList();

        Assert.That(outputs.Count, Is.EqualTo(expectedOutputs));
        if (pruneCopies)
            Assert.That(outputs.Single().Shape.Count, Is.EqualTo(2));
    }

    [TestCase(false, 1)]
    [TestCase(true, 0)]
    public void DisagreeingCopiesAreRemovedOnlyWhenPruning(bool pruneCopies, int expectedOutputs)
    {
        (string first, string second) = FindIncompatibleSegments();
        var anySegment = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        AffixProcessRule rule = CreateCopyRule(Pattern<Word, ShapeNode>.New("copy").Annotation(anySegment).Value);
        var morpher = new Morpher(TraceManager, Language) { PruneDisagreeingCopies = pruneCopies };

        List<Word> outputs = new AnalysisAffixProcessRule(morpher, rule).Apply(CreateInput(first + second)).ToList();

        Assert.That(outputs.Count, Is.EqualTo(expectedOutputs));
    }

    private static AffixProcessRule CreateCopyRule(Pattern<Word, ShapeNode> part)
    {
        var rule = new AffixProcessRule
        {
            Name = "full_copy",
            Gloss = "RED",
            RequiredSyntacticFeatureStruct = FeatureStruct.New().Value,
            OutSyntacticFeatureStruct = FeatureStruct.New().Value,
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph { Lhs = { part }, Rhs = { new CopyFromInput("copy"), new CopyFromInput("copy") } }
        );
        return rule;
    }

    private (string First, string Second) FindIncompatibleSegments()
    {
        CharacterDefinition[] segments = Table3
            .Where(definition => definition.Type == HCFeatureSystem.Segment)
            .ToArray();
        var pair = segments
            .SelectMany(
                (first, index) => segments.Skip(index + 1).Select(second => new { First = first, Second = second })
            )
            .First(candidate => !candidate.First.FeatureStruct.IsUnifiable(candidate.Second.FeatureStruct));
        return (pair.First.Representations.First(), pair.Second.Representations.First());
    }

    private Word CreateInput(string representation)
    {
        var input = new Word(Surface, Table3.Segment(representation)) { AnalysisScope = new AnalysisScope() };
        input.Freeze();
        return input;
    }
}
