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
    public void MismatchingEdgeSkipsMatcher()
    {
        int rejected = 0;
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1)
        {
            EdgePrefilterRejected = () => rejected++,
        };
        var rule = new AnalysisAffixProcessRule(morpher, SuffixRuleAppending("d"));
        Assert.That(rule.Apply(FrozenInput("sag")), Is.Empty);
        Assert.That(rejected, Is.EqualTo(1));
        morpher.EdgePrefilterEnabled = false;
        Assert.That(rule.Apply(FrozenInput("sag")), Is.Empty);
        Assert.That(rejected, Is.EqualTo(1));
    }

    [Test]
    public void RejectedEdgeEmitsTheSameFailureTraceAsMatcher()
    {
        var traceManager = new TraceManager { IsTracing = true };
        AffixProcessRule rule = SuffixRuleAppending("d");
        var input = FrozenInput("sag");
        int rejected = 0;
        var morpher = new Morpher(traceManager, Language, maxDegreeOfParallelism: 1)
        {
            EdgePrefilterRejected = () => rejected++,
        };
        var analysisRule = new AnalysisAffixProcessRule(morpher, rule);

        Trace[] Run(bool enabled)
        {
            morpher.EdgePrefilterEnabled = enabled;
            traceManager.AnalyzeWord(Language, input);
            var root = (Trace)input.CurrentTrace;
            Assert.That(analysisRule.Apply(input), Is.Empty);
            return root.Children.ToArray();
        }

        Trace[] matcher = Run(false);
        Trace[] prefilter = Run(true);
        Assert.That(rejected, Is.EqualTo(1));
        Assert.That(matcher, Has.Length.EqualTo(1));
        Assert.That(prefilter, Has.Length.EqualTo(1));
        foreach (Trace trace in matcher.Concat(prefilter))
        {
            Assert.That(trace.Type, Is.EqualTo(TraceType.MorphologicalRuleAnalysis));
            Assert.That(trace.Source, Is.SameAs(rule));
            Assert.That(trace.SubruleIndex, Is.Zero);
            Assert.That(trace.Input, Is.SameAs(input));
            Assert.That(trace.Output, Is.Null);
        }
    }

    [TestCase("sagd")]
    [TestCase("sast")]
    public void AlphaVariableEdgePassesWithUnboundVariable(string word)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var consonant = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons+")
            .Value;
        var alphaConsonant = FeatureStruct
            .New(Language.PhonologicalFeatureSystem, consonant)
            .Feature("vd")
            .EqualToVariable("a")
            .Value;
        var rule = new AffixProcessRule { Id = "alpha", Name = "alpha" };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(alphaConsonant).Value },
                Rhs = { new CopyFromInput("1"), new InsertSimpleContext(consonant, Variable("vd", "a")) },
            }
        );
        var spec = new AnalysisAffixProcessAllomorphRuleSpec(rule.Allomorphs[0]);
        Assert.That(spec.RightEdgeConstraint.HasVariables, Is.True);
        int rejected = 0;
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1)
        {
            EdgePrefilterRejected = () => rejected++,
        };
        var analysisRule = new AnalysisAffixProcessRule(morpher, rule);
        Word input = FrozenInput(word);
        Word[] enabled = analysisRule.Apply(input).ToArray();
        morpher.EdgePrefilterEnabled = false;
        Word[] disabled = analysisRule.Apply(input).ToArray();
        Assert.That(rejected, Is.Zero);
        Assert.That(enabled, Is.Not.Empty);
        AssertEquivalent(enabled, disabled);
    }

    [Test]
    public void GroupFirstWithOptionalLeadingConstraintDoesNotReject()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule { Id = "optional", Name = "optional" };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>
                        .New("1")
                        .Annotation(Character(Table3, "z"))
                        .Optional.Annotation(any)
                        .OneOrMore.Value,
                },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "d") },
            }
        );
        var spec = new AnalysisAffixProcessAllomorphRuleSpec(rule.Allomorphs[0]);
        Assert.That(spec.Pattern.Children.First, Is.InstanceOf<Group<Word, ShapeNode>>());
        Assert.That(spec.LeftEdgeConstraint, Is.Null);
        int rejected = 0;
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1)
        {
            EdgePrefilterRejected = () => rejected++,
        };
        var analysisRule = new AnalysisAffixProcessRule(morpher, rule);
        Word input = FrozenInput("sagd");
        Word[] enabled = analysisRule.Apply(input).ToArray();
        morpher.EdgePrefilterEnabled = false;
        Word[] disabled = analysisRule.Apply(input).ToArray();
        Assert.That(rejected, Is.Zero);
        Assert.That(enabled, Is.Not.Empty);
        AssertEquivalent(enabled, disabled);
    }

    [Test]
    public void DigraphIsOneSegmentAtTheEdge()
    {
        AffixProcessRule rule = SuffixRuleAppending("ats");
        var spec = new AnalysisAffixProcessAllomorphRuleSpec(rule.Allomorphs[0]);
        Assert.That(spec.RightEdgeConstraint, Is.Not.Null);
        Assert.That(
            Morphophonemic.CharacterDefinitionTable.Segment("sagats").Count(n => n.Type() == HCFeatureSystem.Segment),
            Is.EqualTo(5)
        );
        int rejected = 0;
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1)
        {
            EdgePrefilterRejected = () => rejected++,
        };
        var analysisRule = new AnalysisAffixProcessRule(morpher, rule);
        Word[] enabled = analysisRule.Apply(FrozenInput("sagats")).ToArray();
        Assert.That(enabled, Is.Not.Empty);
        Assert.That(analysisRule.Apply(FrozenInput("sagat")), Is.Empty);
        Assert.That(rejected, Is.EqualTo(1));
        morpher.EdgePrefilterEnabled = false;
        AssertEquivalent(enabled, analysisRule.Apply(FrozenInput("sagats")).ToArray());
        Assert.That(analysisRule.Apply(FrozenInput("sagat")), Is.Empty);
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
    public void BoundaryNodesDoNotHideSegmentEdges()
    {
        AffixProcessRule rule = SuffixRuleAppending("d");
        Word[] enabled = Apply(rule, "+sagd+", true);
        Word[] disabled = Apply(rule, "+sagd+", false);
        AssertEquivalent(enabled, disabled);
        Assert.That(enabled, Is.Not.Empty);
    }

    [Test]
    public void OptionalEdgeNode_PrefilterMatchesFullMatcher()
    {
        AffixProcessRule rule = SuffixRuleAppending("d");

        Word[] enabled = Apply(rule, "sadg", true, optionalLast: true);
        Word[] disabled = Apply(rule, "sadg", false, optionalLast: true);
        AssertEquivalent(enabled, disabled);
        Assert.That(enabled, Is.Not.Empty);
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
