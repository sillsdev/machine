using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

[TestFixture]
public class CopyAgreementPruneTests : HermitCrabTestBase
{
    [Test]
    public void PruningIsOnByDefault()
    {
        Assert.That(new Morpher(TraceManager, Language).PruneDisagreeingCopies, Is.True);
    }

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

    /// <summary>
    /// The prune's weakest point: a self-feeding deletion strips two segments from the second copy only,
    /// and <see cref="Morpher.DeletionReapplications"/> = 0 lets analysis restore just one of them.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    public void CapLimitedDeletionInOneCopyParsesTheSameWithAndWithoutPruning(int deletionReapplications)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var vowel = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("voc+")
            .Value;
        AffixProcessRule redup = CreateCopyRule(Pattern<Word, ShapeNode>.New("copy").Annotation(any).OneOrMore.Value);
        redup.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;
        Morphophonemic.MorphologicalRules.Add(redup);

        var gDelete = new RewriteRule
        {
            Name = "g_delete",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "g")).Value,
        };
        gDelete.Subrules.Add(
            new RewriteSubrule { LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value }
        );
        Allophonic.PhonologicalRules.Add(gDelete);

        LexEntry root = AddEntry(
            "GG",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            Morphophonemic,
            "ggasa"
        );
        try
        {
            var generator = new Morpher(TraceManager, Language);
            Assert.That(
                generator.GenerateWords(root, new Morpheme[] { redup }, FeatureStruct.New().Value),
                Is.EquivalentTo(new[] { "ggasaasa" }),
                "forward derivation defines the expected parse"
            );

            string[] Parse(bool pruneCopies)
            {
                var morpher = new Morpher(TraceManager, Language)
                {
                    DeletionReapplications = deletionReapplications,
                    PruneDisagreeingCopies = pruneCopies,
                };
                return morpher
                    .ParseWord("ggasaasa")
                    .Select(w => string.Join(" ", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Gloss)))
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .ToArray();
            }

            string[] unpruned = Parse(false);
            Assert.That(unpruned, Does.Contain("GG RED"), "the unpruned engine finds the generated analysis");
            Assert.That(Parse(true), Is.EqualTo(unpruned));
        }
        finally
        {
            Morphophonemic.Entries.Remove(root);
        }
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
