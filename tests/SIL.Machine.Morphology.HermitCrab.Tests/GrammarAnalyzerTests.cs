using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public class GrammarAnalyzerTests : HermitCrabTestBase
{
    [Test]
    public void HC0001_NoOvertExponentWithMultipleApplication_IsError()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule
        {
            Name = "bad_rule",
            MaxApplicationCount = 100,
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(rule);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(
            diagnostics,
            Has.Some.Matches<GrammarDiagnostic>(d =>
                d.Code == "HC0001" && d.Severity == DiagnosticSeverity.Error && d.Rule == rule
            )
        );
    }

    [Test]
    public void HC0002_NoOvertExponentSingleApplication_IsWarning()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule
        {
            Name = "zero_exponent_rule",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(rule);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(
            diagnostics,
            Has.Some.Matches<GrammarDiagnostic>(d => d.Code == "HC0002" && d.Severity == DiagnosticSeverity.Warning)
        );
        Assert.That(diagnostics, Has.None.Matches<GrammarDiagnostic>(d => d.Code == "HC0001"));
    }

    [Test]
    public void HC0001_RuleWithOvertExponent_IsNotFlagged()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule
        {
            Name = "ed_suffix",
            MaxApplicationCount = 100,
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(rule);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Has.None.Matches<GrammarDiagnostic>(d => d.Code == "HC0001" || d.Code == "HC0002"));
        // MaxApplicationCount > 1 alone still trips HC0003 regardless of overt exponent.
        Assert.That(diagnostics, Has.Some.Matches<GrammarDiagnostic>(d => d.Code == "HC0003" && d.Rule == rule));
    }

    [Test]
    public void HC0004_SelfFeedingSimultaneousRule_IsFlagged()
    {
        // Matches AnalysisRewriteRule's own ReapplyType.SelfOpaquing selection: Simultaneous mode with
        // a Rhs segment constraint that is NOT unifiable with its own environment.
        var voc = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("voc+")
            .Value;
        var cons = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("voc-")
            .Value;
        var rule = new RewriteRule
        {
            Name = "self_feeding_rule",
            ApplicationMode = RewriteApplicationMode.Simultaneous,
            Lhs = Pattern<Word, int>.New().Value,
        };
        rule.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, int>.New().Annotation(voc).Value,
                LeftEnvironment = Pattern<Word, int>.New().Annotation(cons).Value,
            }
        );
        Allophonic.PhonologicalRules.Add(rule);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Has.Some.Matches<GrammarDiagnostic>(d => d.Code == "HC0004" && d.Rule == rule));
    }

    [Test]
    public void HC0004_SimultaneousEpenthesis_IsUnconditionallyFlagged()
    {
        // Epenthesis (Lhs.Children.Count == 0): the engine (AnalysisRewriteRule's constructor) selects
        // ReapplyType.SelfOpaquing here whenever ApplicationMode is Simultaneous, with no unification
        // check at all — unlike the same-length-subrule case. Must be flagged unconditionally too.
        var voc = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("voc+")
            .Value;
        var rule = new RewriteRule
        {
            Name = "epenthesis_rule",
            ApplicationMode = RewriteApplicationMode.Simultaneous,
            Lhs = Pattern<Word, int>.New().Value, // empty Lhs = epenthesis
        };
        rule.Subrules.Add(new RewriteSubrule { Rhs = Pattern<Word, int>.New().Annotation(voc).Value });
        Allophonic.PhonologicalRules.Add(rule);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Has.Some.Matches<GrammarDiagnostic>(d => d.Code == "HC0004" && d.Rule == rule));
    }

    [Test]
    public void HC0004_IterativeEpenthesis_IsNotFlagged()
    {
        var voc = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("voc+")
            .Value;
        var rule = new RewriteRule
        {
            Name = "epenthesis_rule_iterative",
            ApplicationMode = RewriteApplicationMode.Iterative,
            Lhs = Pattern<Word, int>.New().Value,
        };
        rule.Subrules.Add(new RewriteSubrule { Rhs = Pattern<Word, int>.New().Annotation(voc).Value });
        Allophonic.PhonologicalRules.Add(rule);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Has.None.Matches<GrammarDiagnostic>(d => d.Code == "HC0004"));
    }

    [Test]
    public void HC0005_UnconstrainedDeletion_IsFlagged()
    {
        var highFrontUnrndVowel = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons-")
            .Symbol("voc+")
            .Symbol("high+")
            .Symbol("low-")
            .Symbol("back-")
            .Symbol("round-")
            .Value;
        var rule = new RewriteRule
        {
            Name = "unconstrained_deletion",
            Lhs = Pattern<Word, int>.New().Annotation(highFrontUnrndVowel).Value,
        };
        rule.Subrules.Add(new RewriteSubrule()); // Rhs defaults to empty (deletion), no environment constraints.
        Allophonic.PhonologicalRules.Add(rule);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Has.Some.Matches<GrammarDiagnostic>(d => d.Code == "HC0005" && d.Rule == rule));
    }

    [Test]
    public void HC0005_ConstrainedDeletion_IsNotFlagged()
    {
        var highFrontUnrndVowel = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons-")
            .Symbol("voc+")
            .Symbol("high+")
            .Symbol("low-")
            .Symbol("back-")
            .Symbol("round-")
            .Value;
        var highVowel = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons-")
            .Symbol("voc+")
            .Symbol("high+")
            .Value;
        var rule = new RewriteRule
        {
            Name = "constrained_deletion",
            Lhs = Pattern<Word, int>.New().Annotation(highFrontUnrndVowel).Value,
        };
        rule.Subrules.Add(
            new RewriteSubrule { LeftEnvironment = Pattern<Word, int>.New().Annotation(highVowel).Value }
        );
        Allophonic.PhonologicalRules.Add(rule);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Has.None.Matches<GrammarDiagnostic>(d => d.Code == "HC0005"));
    }

    [Test]
    public void HC0006_UnconstrainedCompounding_IsFlagged()
    {
        var rule = new CompoundingRule { Name = "unconstrained_compound" };
        Morphophonemic.MorphologicalRules.Add(rule);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Has.Some.Matches<GrammarDiagnostic>(d => d.Code == "HC0006" && d.Rule == rule));
    }

    [Test]
    public void HC0006_ConstrainedCompounding_IsNotFlagged()
    {
        var rule = new CompoundingRule
        {
            Name = "constrained_compound",
            HeadRequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            NonHeadRequiredSyntacticFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Symbol("V")
                .Value,
        };
        Morphophonemic.MorphologicalRules.Add(rule);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Has.None.Matches<GrammarDiagnostic>(d => d.Code == "HC0006"));
    }

    [Test]
    public void HC0007_AdjacentOptionalIterativeLexicalPattern_IsFlagged()
    {
        var naturalClass = new NaturalClass(new FeatureStruct()) { Name = "Any" };
        Morphophonemic.CharacterDefinitionTable.AddNaturalClass(naturalClass);
        LexEntry entry = AddEntry("pattern_entry", new FeatureStruct(), Morphophonemic, "([Any])([Any])");

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Has.Some.Matches<GrammarDiagnostic>(d => d.Code == "HC0007" && d.Rule == entry));
    }

    [Test]
    public void HC0008_CyclicFeedingPair_IsFlagged()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var a = new AffixProcessRule
        {
            Name = "cycle_a",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        a.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") },
            }
        );
        var b = new AffixProcessRule
        {
            Name = "cycle_b",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        b.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(a);
        Morphophonemic.MorphologicalRules.Add(b);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Has.Some.Matches<GrammarDiagnostic>(d => d.Code == "HC0008"));
    }

    [Test]
    public void Analyze_WellBehavedGrammar_ProducesNoDiagnostics()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var edSuffix = new AffixProcessRule
        {
            Name = "ed_suffix",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var diagnostics = GrammarAnalyzer.Analyze(Language);

        Assert.That(diagnostics, Is.Empty);
    }
}
