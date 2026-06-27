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
    public void Analyze_BoundedReduplicant_IsRegular()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        // A fixed-size reduplicant: the copied part "1" matches a SINGLE segment (no OneOrMore),
        // so the copy is finite → regular (reclaimable by bounded fold), unlike whole-stem copy.
        var redup = new AffixProcessRule { Name = "credup", Gloss = "PL" };
        redup.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(redup);

        GrammarFstReport report = GrammarFstAdvisor.Analyze(Language);

        GrammarAdvisory escape = report.Escapes.Single(a => a.Rule == "credup");
        // Still slow today (Escape preserved), but regular = FST-reclaimable.
        Assert.That(escape.Severity, Is.EqualTo(GrammarAdvisorySeverity.Escape));
        Assert.That(escape.Regular, Is.True, report.Format());
        Assert.That(report.RegularEscapeCount, Is.EqualTo(1));

        Morphophonemic.MorphologicalRules.Remove(redup);
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
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "a"), new CopyFromInput("2") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(infix);

        GrammarFstReport report = GrammarFstAdvisor.Analyze(Language);

        GrammarAdvisory escape = report.Escapes.Single(a => a.Rule == "infix");
        Assert.That(escape.Issue, Does.Contain("Infixation"));
        // Severity is preserved — infixation is slow in today's engine — but it is regular (the
        // split is pattern-defined), so it carries the reclaim path.
        Assert.That(escape.Severity, Is.EqualTo(GrammarAdvisorySeverity.Escape));
        Assert.That(escape.Regular, Is.True);
        Assert.That(report.Tier, Does.StartWith("Tier 2"));

        Morphophonemic.MorphologicalRules.Remove(infix);
    }

    [Test]
    public void Analyze_HarmonyRewrite_StaysEscapeButIsRegular()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        // A vowel-harmony-style rewrite: bounded LHS/RHS, but an UNBOUNDED left environment
        // ("...anything... ___"). By Kaplan & Kay this is a regular relation, but in today's
        // engine it un-applies at many positions and is slow.
        var harmony = new RewriteRule { Name = "harmony", Lhs = Pattern<Word, ShapeNode>.New().Annotation(any).Value };
        harmony.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>.New().Annotation(any).Value,
                LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(any).OneOrMore.Value,
            }
        );
        Allophonic.PhonologicalRules.Add(harmony);

        GrammarFstReport report = GrammarFstAdvisor.Analyze(Language);

        GrammarAdvisory escape = report.Escapes.Single(a => a.Rule == "harmony");
        // The non-expert sanity check: the headline still WARNS (escape present, not Tier 1) ...
        Assert.That(escape.Severity, Is.EqualTo(GrammarAdvisorySeverity.Escape));
        Assert.That(report.Tier, Does.Not.StartWith("Tier 1"));
        Assert.That(report.EscapeCount, Is.GreaterThanOrEqualTo(1));
        // ... and the reclaim path is reported separately: regular (FST-reclaimable), not "fine".
        Assert.That(escape.Regular, Is.True);
        Assert.That(report.RegularEscapeCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(escape.Advice, Does.Contain("today's engine"));

        Allophonic.PhonologicalRules.Remove(harmony);
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
        // Copying the whole stem (part "1" is OneOrMore) is the one genuinely non-regular case.
        Assert.That(escape.Regular, Is.False);
        Assert.That(after.NonRegularEscapeCount, Is.EqualTo(1));
        // The tier verdict changed: this is the warning a grammar engineer sees.
        Assert.That(after.Tier, Is.Not.EqualTo(before.Tier));
        Assert.That(after.Tier, Does.StartWith("Tier 2"));

        Morphophonemic.MorphologicalRules.Remove(redup);
    }

    [Test]
    public void Analyze_RealizationalReduplication_IsExamined()
    {
        // RealizationalAffixProcessRule also implements IMorphologicalRule and has Allomorphs, so a
        // reduplication encoded on one must be examined and flagged — it was previously skipped.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        Assert.That(GrammarFstAdvisor.Analyze(Language).EscapeCount, Is.EqualTo(0), "baseline has no escapes");

        var redup = new RealizationalAffixProcessRule
        {
            Name = "real_redup",
            Gloss = "INTENS",
            RealizationalFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Feature(Head)
                .EqualTo(head => head.Feature("tense").EqualTo("past"))
                .Value,
        };
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
        Assert.That(after.Escapes.Single().Rule, Is.EqualTo("real_redup"));
        Assert.That(after.Escapes.Single().Issue, Does.Contain("Reduplication"));

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
