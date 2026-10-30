using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

// A vowel-fusion slot copies a part that may match nothing; the look-ahead may test behind it only if it
// restores no segment. Slots are in synthesis order and unapplied last to first.
[TestFixture]
public class AnalysisAffixTemplateFusionTests : HermitCrabTestBase
{
    private static readonly FeatureStruct Any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    private sealed record WalkResult(List<string> Parses, int RuleAttempts, int Violations);

    private static AffixTemplateSlot Slot(bool optional, params MorphemicMorphologicalRule[] rules) =>
        new(rules) { Optional = optional };

    private static AffixTemplate TemplateOf(params AffixTemplateSlot[] slots)
    {
        var template = new AffixTemplate { Name = "fusion_template" };
        foreach (AffixTemplateSlot slot in slots)
            template.Slots.Add(slot);
        return template;
    }

    private static AffixProcessRule RuleOf(
        string name,
        Pattern<Word, ShapeNode>[] lhs,
        params MorphologicalOutputAction[] rhs
    )
    {
        var rule = new AffixProcessRule
        {
            Id = name,
            Name = name,
            Gloss = name,
            RequiredSyntacticFeatureStruct = FeatureStruct.New().Value,
            OutSyntacticFeatureStruct = FeatureStruct.New().Value,
        };
        var allomorph = new AffixProcessAllomorph();
        foreach (Pattern<Word, ShapeNode> part in lhs)
            allomorph.Lhs.Add(part);
        foreach (MorphologicalOutputAction action in rhs)
            allomorph.Rhs.Add(action);
        rule.Allomorphs.Add(allomorph);
        return rule;
    }

    private AffixProcessRule Suffix(string insert) =>
        RuleOf(
            "suffix_" + insert,
            new[] { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
            new CopyFromInput("1"),
            new InsertSegments(Table3, insert)
        );

    // Like Mbugwe's object markers: a prefix whose stem vowel is copied twice, followed by an unbounded run
    // that may be empty.
    private AffixProcessRule FusionPrefix(string name, string before, string between) =>
        RuleOf(
            name,
            new[]
            {
                Pattern<Word, ShapeNode>.New("1").Annotation(Any).Value,
                Pattern<Word, ShapeNode>.New("2").Annotation(Any).ZeroOrMore.Value,
            },
            new InsertSegments(Table3, before),
            new CopyFromInput("1"),
            new InsertSegments(Table3, between),
            new CopyFromInput("1"),
            new CopyFromInput("2")
        );

    // The run is capped at one segment, so an unmatched run is written back as an optional segment.
    private AffixProcessRule BoundedRunSuffix(string insert) =>
        RuleOf(
            "bounded_" + insert,
            new[]
            {
                Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                Pattern<Word, ShapeNode>.New("2").Annotation(Any).Optional.Value,
            },
            new CopyFromInput("1"),
            new InsertSegments(Table3, insert)
        );

    private WalkResult Walk(
        AffixTemplate template,
        string form,
        RequiredSlotLookaheadMode mode,
        int maxDegreeOfParallelism = 1
    )
    {
        int attempts = 0;
        int violations = 0;
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism)
        {
            RequiredSlotLookahead = mode,
            RuleSelector = rule =>
            {
                if (rule is AffixProcessRule)
                    System.Threading.Interlocked.Increment(ref attempts);
                return true;
            },
            RequiredSlotShadow = (kind, t, slot) =>
            {
                if (kind != "fired")
                    System.Threading.Interlocked.Increment(ref violations);
            },
        };
        TraceManager.IsTracing = false;
        var input = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment(form));
        input.Freeze();
        List<string> parses = template
            .CompileAnalysisRule(morpher)
            .Apply(input)
            .Select(w => w.ToString() + " " + string.Join(",", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Gloss)))
            .OrderBy(p => p, System.StringComparer.Ordinal)
            .ToList();
        return new WalkResult(parses, attempts, violations);
    }

    private (WalkResult Off, WalkResult On) WalkBoth(AffixTemplate template, string form)
    {
        WalkResult off = Walk(template, form, RequiredSlotLookaheadMode.Off);
        WalkResult on = Walk(template, form, RequiredSlotLookaheadMode.On);
        Assert.That(on.Parses, Is.EqualTo(off.Parses), "the look-ahead must not change the parses of " + form);
        return (off, on);
    }

    [Test]
    public void Apply_FusionSlotWithAnUnboundedRun_StillTestsTheRequiredSlotBehindIt()
    {
        // -ti is required inside the fusion prefix and "sagu" has no t
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, FusionPrefix("fuse", "k", "a")));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagu");

        Assert.Multiple(() =>
        {
            Assert.That(off.Parses, Is.Empty);
            Assert.That(off.RuleAttempts, Is.GreaterThan(0));
            Assert.That(on.RuleAttempts, Is.EqualTo(0), "the fusion slot restores nothing, so -ti cannot appear");
        });
    }

    [Test]
    public void Apply_FusionSlotWithAnUnboundedRun_KeepsAWordThatHasTheMaterial()
    {
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, FusionPrefix("fuse", "k", "a")));

        foreach (string form in new[] { "kasagti", "ksagti", "sagti", "kasagtiz" })
        {
            (WalkResult off, WalkResult on) = WalkBoth(template, form);
            Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts), form);
        }
    }

    [Test]
    public void Apply_SlotWithABoundedRunThatMayBeEmpty_RestoresASegmentTheInnerSlotCanUse()
    {
        // the run is written back as an optional segment, which can stand for the i of -ti
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, BoundedRunSuffix("z")));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagtz");

        Assert.Multiple(() =>
        {
            Assert.That(on.Parses, Is.Not.Empty, "the parse that needs the restored segment must survive");
            Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts));
        });
    }

    [Test]
    public void Apply_SlotWithABoundedRunThatMayBeEmpty_StaysImpureWhenTheMaterialIsAbsent()
    {
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, BoundedRunSuffix("z")));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagz");

        Assert.That(on.RuleAttempts, Is.GreaterThan(0), "the slot may restore up to one segment, so no pruning");
    }

    [Test]
    public void Apply_EmptyUnboundedRun_RestoresNoSegmentForTheRequiredSlot()
    {
        AffixProcessRule fusion = FusionPrefix("fuse", "k", "a");
        AffixTemplate template = TemplateOf(Slot(false, Suffix("i")), Slot(true, fusion));
        var morpher = new Morpher(TraceManager, Language, 1);
        var input = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment("ktat"));
        input.Freeze();
        var outputs = fusion.CompileAnalysisRule(morpher).Apply(input).ToArray();
        Assert.That(outputs, Has.Length.EqualTo(1));
        Assert.That(outputs[0].Shape.Count(n => n.Type() == HCFeatureSystem.Segment), Is.EqualTo(1));
        Assert.That(outputs[0].Shape.Any(n => n.Annotation.Optional), Is.False);
        (WalkResult off, WalkResult on) = WalkBoth(template, "ktat");
        Assert.That(off.Parses, Is.Empty);
        Assert.That(on.RuleAttempts, Is.EqualTo(0));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Apply_UnboundedRunWithOrderedCopiesOrDeletion_RemainsPure(bool copyRun)
    {
        var actions = new List<MorphologicalOutputAction> { new CopyFromInput("1") };
        if (copyRun)
            actions.Add(new CopyFromInput("2"));
        actions.Add(new InsertSegments(Table3, "z"));
        AffixProcessRule outer = RuleOf(
            "outer",
            new[]
            {
                Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                Pattern<Word, ShapeNode>.New("2").Annotation(Any).ZeroOrMore.Value,
            },
            actions.ToArray()
        );
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, outer));
        (WalkResult off, WalkResult on) = WalkBoth(template, "sagz");
        Assert.That(off.RuleAttempts, Is.GreaterThan(0));
        Assert.That(on.RuleAttempts, Is.EqualTo(0));
    }

    [TestCase(1)]
    [TestCase(0)]
    public void Apply_OnEveryFusionTemplateAndWord_KeepsTheParsesAndNeverViolatesTheShadow(int maxDegreeOfParallelism)
    {
        var templates = new Dictionary<AffixTemplate, string[]>
        {
            [TemplateOf(Slot(false, Suffix("ti")), Slot(true, FusionPrefix("fuse", "k", "a")))] = new[]
            {
                "kasagti",
                "ksagti",
                "sagu",
                "sagti",
            },
            [
                TemplateOf(
                    Slot(false, Suffix("ti")),
                    Slot(false, FusionPrefix("fuse", "k", "a")),
                    Slot(true, Suffix("z"))
                )
            ] = new[] { "kasagtiz", "kasagtzi", "kasagz" },
            [TemplateOf(Slot(false, Suffix("ti")), Slot(true, BoundedRunSuffix("z")))] = new[]
            {
                "sagtz",
                "sagz",
                "sagtiz",
            },
            [
                TemplateOf(
                    Slot(false, Suffix("u")),
                    Slot(false, FusionPrefix("fuse", "k", "a")),
                    Slot(false, Suffix("ti"))
                )
            ] = new[] { "kasagtiu", "kasaguti", "sagtiu" },
        };

        foreach ((AffixTemplate template, string[] forms) in templates)
        {
            foreach (string form in forms)
            {
                WalkResult off = Walk(template, form, RequiredSlotLookaheadMode.Off, maxDegreeOfParallelism);
                WalkResult on = Walk(template, form, RequiredSlotLookaheadMode.On, maxDegreeOfParallelism);
                WalkResult shadow = Walk(template, form, RequiredSlotLookaheadMode.Shadow, maxDegreeOfParallelism);
                Assert.Multiple(() =>
                {
                    Assert.That(on.Parses, Is.EqualTo(off.Parses), form);
                    Assert.That(shadow.Parses, Is.EqualTo(off.Parses), form);
                    Assert.That(shadow.Violations, Is.EqualTo(0), form);
                });
            }
        }
    }
}
