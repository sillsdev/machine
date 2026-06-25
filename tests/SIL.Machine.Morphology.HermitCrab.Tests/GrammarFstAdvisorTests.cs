using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Verifies the grammar linter (<see cref="GrammarFstAdvisor"/>): a plain concatenative grammar
/// is a Tier 1 (fully FST-able) candidate with no escapes, and adding a single reduplication rule
/// flips the verdict — the offending rule is flagged <see cref="GrammarAdvisorySeverity.Escape"/>
/// with a reduplication write-up. This is the "one new rule blew up the grammar" guard.
/// </summary>
public class GrammarFstAdvisorTests : HermitCrabTestBase
{
    [Test]
    public void Analyze_ConcatenativeGrammar_Tier1NoEscapes()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        // A plain suffix: copy the whole stem, then add segments. Fully finite-state.
        var sSuffix = new AffixProcessRule { Name = "s_suffix", Gloss = "PL" };
        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(sSuffix);

        // A suffix over a SPLIT stem (copy part 1, copy part 2, then insert): the copies are
        // contiguous, so this is an ordinary suffix — finite-state, must NOT be flagged.
        var splitSuffix = new AffixProcessRule { Name = "split_suffix", Gloss = "PST" };
        splitSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(any).Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value,
                },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new InsertSegments(Table3, "d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(splitSuffix);

        GrammarFstReport report = GrammarFstAdvisor.Analyze(Language);

        Assert.That(report.EscapeCount, Is.EqualTo(0), report.Format());
        Assert.That(report.Tier, Does.StartWith("Tier 1"));

        Morphophonemic.MorphologicalRules.Remove(sSuffix);
        Morphophonemic.MorphologicalRules.Remove(splitSuffix);
    }

    [Test]
    public void Analyze_TrueInfix_FlaggedEscape()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        // Infixation: insert material BETWEEN two copies of the stem (copy…insert…copy).
        var infix = new AffixProcessRule { Name = "infix", Gloss = "PERF" };
        infix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(any).Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value,
                },
                Rhs =
                {
                    new CopyFromInput("1"),
                    new InsertSegments(Table3, "a"),
                    new CopyFromInput("2"),
                },
            }
        );
        Morphophonemic.MorphologicalRules.Add(infix);

        GrammarFstReport report = GrammarFstAdvisor.Analyze(Language);

        GrammarAdvisory escape = report.Escapes.Single(a => a.Rule == "infix");
        Assert.That(escape.Issue, Does.Contain("Infixation"));
        Assert.That(report.Tier, Does.StartWith("Tier 2"));

        Morphophonemic.MorphologicalRules.Remove(infix);
    }

    [Test]
    public void Analyze_ReduplicationRule_FlaggedEscapeAndTierDowngraded()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        GrammarFstReport before = GrammarFstAdvisor.Analyze(Language);
        Assert.That(before.EscapeCount, Is.EqualTo(0), "baseline grammar should have no escapes");

        // Total reduplication: copy the stem ("1") twice. Copying an unbounded span is not
        // finite-state — exactly the rule that should blow up the grammar.
        var redup = new AffixProcessRule { Name = "redup", Gloss = "INTENS" };
        redup.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(redup);

        GrammarFstReport after = GrammarFstAdvisor.Analyze(Language);

        Assert.That(after.EscapeCount, Is.EqualTo(1), after.Format());
        GrammarAdvisory escape = after.Escapes.Single();
        Assert.That(escape.Rule, Is.EqualTo("redup"));
        Assert.That(escape.Severity, Is.EqualTo(GrammarAdvisorySeverity.Escape));
        Assert.That(escape.Issue, Does.Contain("Reduplication"));
        Assert.That(escape.Advice, Is.Not.Empty);
        // No phonological rule applies after it, so the escape is probe-able (clean).
        Assert.That(escape.Probeable, Is.True);
        Assert.That(after.ProbeableEscapeCount, Is.EqualTo(1));
        // The tier verdict changed: this is the warning a grammar engineer sees.
        Assert.That(after.Tier, Is.Not.EqualTo(before.Tier));
        Assert.That(after.Tier, Does.StartWith("Tier 2"));

        Morphophonemic.MorphologicalRules.Remove(redup);
    }

    [Test]
    public void Analyze_ReduplicationWithLaterPhonology_IsOpaque()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var redup = new AffixProcessRule { Name = "redup", Gloss = "INTENS" };
        redup.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(redup);

        // A phonological rule in a LATER stratum can rewrite the reduplicated span, so the
        // strip-and-reparse probe is no longer sound — the escape is opaque (needs the backstop).
        var rule = new RewriteRule { Name = "t_rule", Lhs = Pattern<Word, ShapeNode>.New().Annotation(any).Value };
        Surface.PhonologicalRules.Add(rule);

        GrammarFstReport report = GrammarFstAdvisor.Analyze(Language);

        GrammarAdvisory escape = report.Escapes.Single(a => a.Rule == "redup");
        Assert.That(escape.Probeable, Is.False, report.Format());
        Assert.That(report.OpaqueEscapeCount, Is.EqualTo(1));
        Assert.That(report.Tier, Does.StartWith("Tier 2 candidate — hybrid"));

        Morphophonemic.MorphologicalRules.Remove(redup);
        Surface.PhonologicalRules.Remove(rule);
    }
}
