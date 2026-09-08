using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

[TestFixture]
public class AnalysisEdgePrefilterTests : HermitCrabTestBase
{
    private AffixProcessRule SuffixRuleAppending(string literal)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule
        {
            Id = "S1",
            Name = "s1_suffix",
            Gloss = "S1",
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, literal) },
            }
        );
        return rule;
    }

    private AffixProcessRule PrefixRulePrepending(string literal)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule
        {
            Id = "P1",
            Name = "p1_prefix",
            Gloss = "P1",
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, literal), new CopyFromInput("1") },
            }
        );
        return rule;
    }

    private AffixProcessRule CircumfixRule(string prefixLiteral, string suffixLiteral)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule
        {
            Id = "C1",
            Name = "c1_circumfix",
            Gloss = "C1",
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs =
                {
                    new InsertSegments(Table3, prefixLiteral),
                    new CopyFromInput("1"),
                    new InsertSegments(Table3, suffixLiteral),
                },
            }
        );
        return rule;
    }

    private Word FrozenInput(string word, bool optionalLast = false)
    {
        Shape shape = Morphophonemic.CharacterDefinitionTable.Segment(word);
        if (optionalLast)
            shape.Last.Annotation.Optional = true;
        var input = new Word(Morphophonemic, shape);
        input.Freeze();
        return input;
    }

    private Word[] Apply(AffixProcessRule rule, string word, bool enabled, bool optionalLast = false)
    {
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1) { EdgePrefilterEnabled = enabled };
        return new AnalysisAffixProcessRule(morpher, rule).Apply(FrozenInput(word, optionalLast)).ToArray();
    }

    private static void AssertEquivalent(Word[] enabled, Word[] disabled)
    {
        Assert.That(
            enabled.Select(w => w.ToString()).OrderBy(s => s, StringComparer.Ordinal),
            Is.EqualTo(disabled.Select(w => w.ToString()).OrderBy(s => s, StringComparer.Ordinal))
        );
    }

    [Test]
    public void SuffixEdgeMismatch_PrefilterMatchesFullMatcher()
    {
        AffixProcessRule rule = SuffixRuleAppending("d");

        Word[] enabled = Apply(rule, "sag", true);
        Word[] disabled = Apply(rule, "sag", false);

        AssertEquivalent(enabled, disabled);
        Assert.That(enabled, Is.Empty);
    }

    [Test]
    public void SuffixEdgeMatch_PrefilterMatchesFullMatcher()
    {
        AffixProcessRule rule = SuffixRuleAppending("d");

        Word[] enabled = Apply(rule, "sagd", true);
        Word[] disabled = Apply(rule, "sagd", false);

        AssertEquivalent(enabled, disabled);
        Assert.That(enabled, Is.Not.Empty);
    }

    [Test]
    public void OptionalEdgeNode_PrefilterMatchesFullMatcher()
    {
        AffixProcessRule rule = SuffixRuleAppending("d");

        AssertEquivalent(Apply(rule, "sag", true, optionalLast: true), Apply(rule, "sag", false, optionalLast: true));
    }

    [TestCase("sag", false)]
    [TestCase("zag", true)]
    public void PrefixEdge_PrefilterMatchesFullMatcher(string word, bool matches)
    {
        AffixProcessRule rule = PrefixRulePrepending("z");

        Word[] enabled = Apply(rule, word, true);
        Word[] disabled = Apply(rule, word, false);

        AssertEquivalent(enabled, disabled);
        Assert.That(enabled.Any(), Is.EqualTo(matches));
    }

    [TestCase("sag", false)]
    [TestCase("zag", false)]
    [TestCase("sad", false)]
    [TestCase("zad", true)]
    public void CircumfixEdges_PrefilterMatchesFullMatcher(string word, bool matches)
    {
        AffixProcessRule rule = CircumfixRule("z", "d");

        Word[] enabled = Apply(rule, word, true);
        Word[] disabled = Apply(rule, word, false);

        AssertEquivalent(enabled, disabled);
        Assert.That(enabled.Any(), Is.EqualTo(matches));
    }

    [TestCase("sag", true)]
    [TestCase("sas", false)]
    public void NaturalClassEdge_PrefilterMatchesFullMatcher(string word, bool matches)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var voiced = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("vd+")
            .Value;
        var rule = new AffixProcessRule
        {
            Id = "NC1",
            Name = "nc1_suffix",
            Gloss = "NC1",
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSimpleContext(voiced) },
            }
        );

        Word[] enabled = Apply(rule, word, true);
        Word[] disabled = Apply(rule, word, false);

        AssertEquivalent(enabled, disabled);
        Assert.That(enabled.Any(), Is.EqualTo(matches));
    }
}
