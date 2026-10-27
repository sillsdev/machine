using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

public class LeanTraceTests : HermitCrabTestBase
{
    private static readonly string[] Words =
    {
        "sagd",
        "sagdv",
        "sagdvs",
        "sagzv",
        "sid",
        "sau",
        "pʰitʰ",
        "datʰ",
        "gab",
        "sagdvsagd",
        "datpʰut",
        "pʰutdat",
        "sagpʰut",
    };

    [Test]
    public void LeanTrace_MatchesFullTraceNodeForNode()
    {
        BuildScenario();
        var seenTypes = new HashSet<TraceType>();
        var seenReasons = new HashSet<FailureReason>();
        int nodes = 0;
        foreach (string word in Words)
        {
            Trace full = ParseWithTrace(word, retainWords: true);
            Trace lean = ParseWithTrace(word, retainWords: false);
            nodes += AssertSameTree(full, lean, seenTypes, seenReasons);
        }

        Assert.That(nodes, Is.GreaterThan(100));
        TraceType[] expectedTypes =
        {
            TraceType.WordAnalysis,
            TraceType.StratumAnalysisInput,
            TraceType.StratumAnalysisOutput,
            TraceType.PhonologicalRuleAnalysis,
            TraceType.PhonologicalRuleSynthesis,
            TraceType.TemplateAnalysisInput,
            TraceType.TemplateAnalysisOutput,
            TraceType.TemplateSynthesisInput,
            TraceType.TemplateSynthesisOutput,
            TraceType.MorphologicalRuleAnalysis,
            TraceType.MorphologicalRuleSynthesis,
            TraceType.CompoundingRuleAnalysis,
            TraceType.LexicalLookup,
            TraceType.WordSynthesis,
            TraceType.StratumSynthesisInput,
            TraceType.StratumSynthesisOutput,
            TraceType.Successful,
            TraceType.Failed,
            TraceType.Blocked,
        };
        foreach (TraceType type in expectedTypes)
            Assert.That(seenTypes, Does.Contain(type), type.ToString());
        Assert.That(seenReasons.Count, Is.GreaterThan(1));
    }

    [Test]
    public void LeanTrace_KeepsNoWords()
    {
        BuildScenario();
        foreach (string word in Words)
        {
            Trace lean = ParseWithTrace(word, retainWords: false);
            AssertNoWords(lean);
        }
    }

    [Test]
    public void FullTrace_StillKeepsWords()
    {
        BuildScenario();
        Trace full = ParseWithTrace("sagd", retainWords: true);
        Assert.That(full.Input, Is.Not.Null);
        Assert.That(Flatten(full).Any(t => t.Output != null), Is.True);
    }

    [Test]
    public void ParseWord_IsIdenticalWithTracingOffFullAndLean()
    {
        BuildScenario();
        foreach (string word in Words)
        {
            var off = new Morpher(new TraceManager { IsTracing = false }, Language, 1);
            string expected = Describe(off.ParseWord(word));
            Assert.That(Describe(ParseWords(word, retainWords: true)), Is.EqualTo(expected), word);
            Assert.That(Describe(ParseWords(word, retainWords: false)), Is.EqualTo(expected), word);
        }
    }

    [Test]
    public void FrozenWord_CachesItsTraceTextAndCopiesDoNotInherit()
    {
        BuildScenario();
        var morpher = new Morpher(new TraceManager(), Language, 1);
        Word word = morpher.ParseWord("sagd").First();
        Assert.That(word.IsFrozen, Is.True);
        string first = word.GetTraceText(false);
        Assert.That(word.GetTraceText(false), Is.SameAs(first));
        Assert.That(word.GetTraceText(true), Is.Not.Null);

        Word copy = word.Clone();
        Assert.That(copy.IsFrozen, Is.False);
        Assert.That(copy.GetTraceText(false), Is.EqualTo(first));
        Assert.That(copy.GetTraceText(false), Is.Not.SameAs(first));
    }

    private Trace ParseWithTrace(string word, bool retainWords)
    {
        var manager = new TraceManager { IsTracing = true, RetainWords = retainWords };
        var morpher = new Morpher(manager, Language, 1);
        morpher.ParseWord(word, out object trace);
        return (Trace)trace;
    }

    private IEnumerable<Word> ParseWords(string word, bool retainWords)
    {
        var manager = new TraceManager { IsTracing = true, RetainWords = retainWords };
        return new Morpher(manager, Language, 1).ParseWord(word).ToArray();
    }

    private static string Describe(IEnumerable<Word> words)
    {
        return string.Join(
            "|",
            words
                .Select(w =>
                    w.Shape.ToString(w.Stratum.CharacterDefinitionTable, true)
                    + ":"
                    + string.Join(" ", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Gloss))
                )
                .OrderBy(s => s, StringComparer.Ordinal)
        );
    }

    private static IEnumerable<Trace> Flatten(Trace trace)
    {
        yield return trace;
        foreach (Trace child in trace.Children)
        {
            foreach (Trace descendant in Flatten(child))
                yield return descendant;
        }
    }

    private static void AssertNoWords(Trace trace)
    {
        foreach (Trace node in Flatten(trace))
        {
            Assert.That(node.Input, Is.Null);
            Assert.That(node.Output, Is.Null);
        }
    }

    private static int AssertSameTree(
        Trace full,
        Trace lean,
        HashSet<TraceType> seenTypes,
        HashSet<FailureReason> seenReasons
    )
    {
        Assert.That(lean.Input, Is.Null);
        Assert.That(lean.Output, Is.Null);
        Assert.That(lean.Type, Is.EqualTo(full.Type));
        Assert.That(lean.Source, Is.SameAs(full.Source));
        Assert.That(lean.SubruleIndex, Is.EqualTo(full.SubruleIndex));
        Assert.That(lean.FailureReason, Is.EqualTo(full.FailureReason));
        Assert.That(lean.InputText, Is.EqualTo(full.InputText));
        Assert.That(lean.OutputText, Is.EqualTo(full.OutputText));
        Assert.That(full.InputText, Is.EqualTo(PreviousCliText(full.Type, full.Input)));
        Assert.That(full.OutputText, Is.EqualTo(PreviousCliText(full.Type, full.Output)));
        Assert.That(lean.Children.Count, Is.EqualTo(full.Children.Count));
        seenTypes.Add(full.Type);
        seenReasons.Add(full.FailureReason);

        int count = 1;
        Trace[] fullChildren = full.Children.ToArray();
        Trace[] leanChildren = lean.Children.ToArray();
        for (int i = 0; i < fullChildren.Length; i++)
            count += AssertSameTree(fullChildren[i], leanChildren[i], seenTypes, seenReasons);
        return count;
    }

    // The rule the CLI used before it read InputText and OutputText.
    private static string? PreviousCliText(TraceType type, Word word)
    {
        if (word == null)
            return null;
        bool analysis =
            type
            is TraceType.WordAnalysis
                or TraceType.LexicalLookup
                or TraceType.StratumAnalysisInput
                or TraceType.StratumAnalysisOutput
                or TraceType.TemplateAnalysisInput
                or TraceType.TemplateAnalysisOutput
                or TraceType.MorphologicalRuleAnalysis
                or TraceType.PhonologicalRuleAnalysis;
        CharacterDefinitionTable table = word.Stratum.CharacterDefinitionTable;
        return analysis ? word.Shape.ToRegexString(table, true) : word.Shape.ToString(table, true);
    }

    private void BuildScenario()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var asp = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("asp+")
            .Value;
        var nonCons = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons-")
            .Value;

        var rule1 = new RewriteRule
        {
            Name = "rule1",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "t")).Value,
        };
        Allophonic.PhonologicalRules.Add(rule1);
        rule1.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
                LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(nonCons).Value,
            }
        );
        var rule2 = new RewriteRule
        {
            Name = "rule2",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table3, "p")).Value,
        };
        Allophonic.PhonologicalRules.Add(rule2);
        Morphophonemic.PhonologicalRules.Add(rule2);
        rule2.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
                RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(nonCons).Value,
            }
        );

        var edSuffix = new RealizationalAffixProcessRule
        {
            Name = "ed_suffix",
            RealizationalFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Feature(Head)
                .EqualTo(head => head.Feature("tense").EqualTo("past"))
                .Value,
            Gloss = "PAST",
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "d") },
            }
        );
        var sSuffix = new RealizationalAffixProcessRule
        {
            Name = "s_suffix",
            RealizationalFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Feature(Head)
                .EqualTo(head => head.Feature("pers").EqualTo("3").Feature("tense").EqualTo("pres"))
                .Value,
            Gloss = "3SG",
        };
        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "z") },
            }
        );
        var evidential = new RealizationalAffixProcessRule
        {
            Name = "evidential",
            RealizationalFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Feature(Head)
                .EqualTo(head => head.Feature("evidential").EqualTo("witnessed"))
                .Value,
            Gloss = "WIT",
        };
        evidential.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "v") },
            }
        );
        var verbTemplate = new AffixTemplate
        {
            Name = "verb",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        verbTemplate.Slots.Add(new AffixTemplateSlot(sSuffix, edSuffix) { Optional = true });
        verbTemplate.Slots.Add(new AffixTemplateSlot(evidential) { Optional = true });
        Morphophonemic.AffixTemplates.Add(verbTemplate);

        var nominalizer = new AffixProcessRule
        {
            Name = "nominalizer",
            Gloss = "NOM",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        nominalizer.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(nominalizer);

        var crule = new CompoundingRule
        {
            Name = "compound",
            HeadRequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            NonHeadRequiredSyntacticFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Symbol("N")
                .Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        crule.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") },
            }
        );
        Allophonic.MorphologicalRules.Add(crule);

        var restricted = new CompoundingRule
        {
            Name = "restricted_compound",
            HeadRequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            NonHeadRequiredSyntacticFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Symbol("N")
                .Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        restricted.NonHeadProdRestrictionsMprFeatures.Add(Latinate);
        restricted.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") },
            }
        );
        Allophonic.MorphologicalRules.Add(restricted);
    }
}
