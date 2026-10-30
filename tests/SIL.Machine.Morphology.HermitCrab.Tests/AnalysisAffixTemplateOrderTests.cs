using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

// Order and edge tests of the required-slot look-ahead; slots are in synthesis order and unapplied last to first.
[TestFixture]
public class AnalysisAffixTemplateOrderTests : HermitCrabTestBase
{
    private static readonly FeatureStruct Any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    private sealed record WalkResult(List<string> Parses, int RuleAttempts, int Violations);

    private static AffixTemplateSlot Slot(bool optional, params MorphemicMorphologicalRule[] rules) =>
        new(rules) { Optional = optional };

    private static AffixTemplate TemplateOf(params AffixTemplateSlot[] slots)
    {
        var template = new AffixTemplate { Name = "order_template" };
        foreach (AffixTemplateSlot slot in slots)
            template.Slots.Add(slot);
        return template;
    }

    private static Pattern<Word, ShapeNode> Part(string name) =>
        Pattern<Word, ShapeNode>.New(name).Annotation(Any).OneOrMore.Value;

    private static Pattern<Word, ShapeNode> OneSegment(string name) =>
        Pattern<Word, ShapeNode>.New(name).Annotation(Any).Value;

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
        RuleOf("suffix_" + insert, new[] { Part("1") }, new CopyFromInput("1"), new InsertSegments(Table3, insert));

    private AffixProcessRule Prefix(string insert) =>
        RuleOf("prefix_" + insert, new[] { Part("1") }, new InsertSegments(Table3, insert), new CopyFromInput("1"));

    private AffixProcessRule Circumfix(string before, string after) =>
        RuleOf(
            "circumfix_" + before + after,
            new[] { Part("1") },
            new InsertSegments(Table3, before),
            new CopyFromInput("1"),
            new InsertSegments(Table3, after)
        );

    // inserts the material after the first segment of the stem
    private AffixProcessRule Infix(string insert) =>
        RuleOf(
            "infix_" + insert,
            new[] { OneSegment("1"), Part("2") },
            new CopyFromInput("1"),
            new InsertSegments(Table3, insert),
            new CopyFromInput("2")
        );

    // writes the last segment of the stem twice
    private static AffixProcessRule RepeatLast() =>
        RuleOf(
            "repeat_last",
            new[] { Part("1"), OneSegment("2") },
            new CopyFromInput("1"),
            new CopyFromInput("2"),
            new CopyFromInput("2")
        );

    // swaps the last two segments of the stem, so it copies its parts out of order
    private static AffixProcessRule Metathesis() =>
        RuleOf(
            "metathesis",
            new[] { Part("1"), OneSegment("2"), OneSegment("3") },
            new CopyFromInput("1"),
            new CopyFromInput("3"),
            new CopyFromInput("2")
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
    public void Apply_RequiredSuffixesInTheWrongOrder_PrunesAtEntry()
    {
        // both literals are present in "saguti", but the outer suffix -u has to end the word
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(false, Suffix("u")));

        (WalkResult off, WalkResult wrong) = WalkBoth(template, "saguti");
        (WalkResult offRight, WalkResult right) = WalkBoth(template, "sagtiu");

        Assert.Multiple(() =>
        {
            Assert.That(off.Parses, Is.Empty);
            Assert.That(off.RuleAttempts, Is.GreaterThan(0));
            Assert.That(wrong.RuleAttempts, Is.EqualTo(0));
            Assert.That(right.Parses, Is.Not.Empty);
            Assert.That(right.RuleAttempts, Is.EqualTo(offRight.RuleAttempts));
        });
    }

    [Test]
    public void Apply_PrefixSlotWhoseMaterialIsNotAtTheLeftEdge_PrunesAtEntry()
    {
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(false, Prefix("ka")));

        (WalkResult off, WalkResult wrong) = WalkBoth(template, "sagkati");
        (WalkResult offRight, WalkResult right) = WalkBoth(template, "kasagti");

        Assert.Multiple(() =>
        {
            Assert.That(off.RuleAttempts, Is.GreaterThan(0));
            Assert.That(wrong.RuleAttempts, Is.EqualTo(0));
            Assert.That(right.Parses, Is.Not.Empty);
            Assert.That(right.RuleAttempts, Is.EqualTo(offRight.RuleAttempts));
        });
    }

    [Test]
    public void Apply_CircumfixWhoseSuffixPartIsNotAtTheRightEdge_PrunesAtEntry()
    {
        AffixTemplate template = TemplateOf(Slot(false, Circumfix("ka", "ti")));

        (WalkResult off, WalkResult wrong) = WalkBoth(template, "kasagtz");
        (WalkResult offRight, WalkResult right) = WalkBoth(template, "kasagti");

        Assert.Multiple(() =>
        {
            Assert.That(wrong.RuleAttempts, Is.EqualTo(0));
            Assert.That(off.RuleAttempts, Is.GreaterThan(0));
            Assert.That(right.Parses, Is.Not.Empty);
            Assert.That(right.RuleAttempts, Is.EqualTo(offRight.RuleAttempts));
        });
    }

    [Test]
    public void Apply_CircumfixPartsThatWouldShareANode_PrunesAtEntry()
    {
        // ka- and -ak could both use the a of "kak"
        AffixTemplate template = TemplateOf(Slot(false, Circumfix("ka", "ak")));

        (WalkResult off, WalkResult on) = WalkBoth(template, "kak");

        Assert.That(on.RuleAttempts, Is.EqualTo(0));
        Assert.That(off.RuleAttempts, Is.GreaterThan(0));
    }

    // an infix does not anchor, so only the window left by the outer required prefix can rule out its material
    private AffixTemplate InfixInsidePrefixTemplate() =>
        TemplateOf(Slot(false, Infix("ak")), Slot(false, Prefix("ka")));

    [Test]
    public void Apply_InnerMaterialOnlyInsideTheOuterPrefix_PrunesAtEntry()
    {
        // the a of "ka" is the only a before the k that -ak needs, so the infix has no material of its own
        (WalkResult off, WalkResult on) = WalkBoth(InfixInsidePrefixTemplate(), "kakg");

        Assert.Multiple(() =>
        {
            Assert.That(off.RuleAttempts, Is.GreaterThan(0));
            Assert.That(on.RuleAttempts, Is.EqualTo(0));
        });
    }

    [Test]
    public void Apply_InnerMaterialOnlyInsideTheOuterSuffix_PrunesAtEntry()
    {
        AffixTemplate template = TemplateOf(Slot(false, Infix("ak")), Slot(false, Suffix("ka")));
        (WalkResult off, WalkResult on) = WalkBoth(template, "gaka");
        Assert.That(off.RuleAttempts, Is.GreaterThan(0));
        Assert.That(on.RuleAttempts, Is.EqualTo(0));
    }

    [Test]
    public void Apply_BoundaryOnlyInfix_DoesNotMakeReachUnbounded()
    {
        var outer = RuleOf(
            "boundary",
            new[] { OneSegment("1"), Part("2") },
            new CopyFromInput("1"),
            new InsertSegments(Table3, "+"),
            new CopyFromInput("2")
        );
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, outer));
        (WalkResult off, WalkResult on) = WalkBoth(template, "sagtig");
        Assert.That(off.RuleAttempts, Is.GreaterThan(0));
        Assert.That(on.RuleAttempts, Is.EqualTo(0));
    }

    [TestCase(1)]
    [TestCase(0)]
    public void Apply_DeletedOptionalSegmentBeforePrefix_DoesNotCountAgainstReach(int parallelism)
    {
        var deletion = RuleOf(
            "delete",
            new[] { Pattern<Word, ShapeNode>.New("1").Annotation(Any).Optional.Value, Part("2") },
            new CopyFromInput("2")
        );
        var morpher = new Morpher(TraceManager, Language, parallelism);
        var input = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment("kasag"));
        input.Freeze();
        Word restored = deletion.CompileAnalysisRule(morpher).Apply(input).Single();
        Assert.That(restored.Shape.First(n => n.Type() == HCFeatureSystem.Segment).Annotation.Optional, Is.True);
        var template = TemplateOf(Slot(false, Prefix("ka")));
        var parses = new List<string[]>();
        foreach (
            RequiredSlotLookaheadMode mode in new[]
            {
                RequiredSlotLookaheadMode.Off,
                RequiredSlotLookaheadMode.On,
                RequiredSlotLookaheadMode.Shadow,
            }
        )
        {
            morpher.RequiredSlotLookahead = mode;
            parses.Add(
                template
                    .CompileAnalysisRule(morpher)
                    .Apply(restored)
                    .Select(w => w.ToString())
                    .OrderBy(x => x, System.StringComparer.Ordinal)
                    .ToArray()
            );
        }
        Assert.That(parses[0], Is.Not.Empty);
        Assert.That(parses[1], Is.EqualTo(parses[0]));
        Assert.That(parses[2], Is.EqualTo(parses[0]));
    }

    [Test]
    public void Apply_InnerMaterialAfterTheOuterPrefix_IsNotPruned()
    {
        // "sag" with the infix ak is "sakag", and ka- in front gives "kasakag"
        (WalkResult off, WalkResult on) = WalkBoth(InfixInsidePrefixTemplate(), "kasakag");

        Assert.Multiple(() =>
        {
            Assert.That(on.Parses, Is.Not.Empty);
            Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts));
        });
    }

    [Test]
    public void Apply_OptionalOuterPrefix_CountsTowardsTheLeftEdgeBudget()
    {
        AffixTemplate template = TemplateOf(Slot(false, Prefix("ka")), Slot(true, Prefix("ba")));

        (WalkResult off, WalkResult on) = WalkBoth(template, "bakasag");

        Assert.That(on.Parses, Is.Not.Empty);
    }

    [Test]
    public void Apply_OuterSlotWithNullAffix_IsNeverPrunedAndTakesNoEdgeMaterial()
    {
        AffixProcessRule uOrNull = Suffix("u");
        uOrNull.Allomorphs.Add(new AffixProcessAllomorph { Lhs = { Part("1") }, Rhs = { new CopyFromInput("1") } });
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(false, uOrNull));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagti");

        Assert.Multiple(() =>
        {
            Assert.That(on.Parses, Is.Not.Empty);
            Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts));
        });
    }

    [Test]
    public void Apply_OptionalOuterSlot_CountsTowardsTheEdgeBudget()
    {
        // the optional -z may stand between -ti and the end of the word
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, Suffix("z")));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagtiz");

        Assert.That(on.Parses, Is.Not.Empty);
    }

    [Test]
    public void Apply_InfixSlotBetween_DoesNotAnchorTheInnerSlot()
    {
        // unapplying the infix z of "kzasag" gives "kasag", which starts with ka-
        AffixTemplate template = TemplateOf(Slot(false, Prefix("ka")), Slot(true, Infix("z")));

        (WalkResult off, WalkResult on) = WalkBoth(template, "kzasag");

        Assert.That(on.Parses, Is.Not.Empty);
    }

    [Test]
    public void Apply_SlotThatCopiesOutOfOrderBetween_ChecksOnlyPresence()
    {
        // unapplying the metathesis of "sagit" gives "sagti", whose -ti is in the opposite order
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, Metathesis()));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagit");

        Assert.That(on.Parses, Is.Not.Empty);
    }

    [Test]
    public void Apply_PartCopiedTwiceBetween_DoesNotAnchorTheInnerSlot()
    {
        // unapplying the repeated i of "sagtii" gives "sagti", which ends in -ti
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, RepeatLast()));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagtii");

        Assert.That(on.Parses, Is.Not.Empty);
    }

    [Test]
    public void Apply_ConstraintWithVariables_IsMatchedByAnySegment()
    {
        var context = new SimpleContext(new NaturalClass(Any), new[] { Variable("vd", "a") });
        AffixProcessRule suffix = RuleOf(
            "variable_suffix",
            new[] { Part("1") },
            new CopyFromInput("1"),
            new InsertSimpleContext(context)
        );
        AffixTemplate template = TemplateOf(Slot(false, suffix));

        foreach (string form in new[] { "sagd", "sagt", "sag" })
            WalkBoth(template, form);
    }

    [TestCase(1)]
    [TestCase(0)]
    public void Apply_OnEveryTemplateAndWord_KeepsTheParsesAndNeverViolatesTheShadow(int maxDegreeOfParallelism)
    {
        var templates = new Dictionary<AffixTemplate, string[]>
        {
            [TemplateOf(Slot(false, Suffix("ti")), Slot(false, Suffix("u")))] = new[] { "saguti", "sagtiu", "sagti" },
            [TemplateOf(Slot(false, Suffix("ti")), Slot(false, Prefix("ka")))] = new[] { "kasagti", "sagkati" },
            [TemplateOf(Slot(false, Circumfix("ka", "ti")), Slot(true, Suffix("z")), Slot(false, Prefix("ba")))] = new[]
            {
                "bakasagtiz",
                "kabasagti",
                "bakasagtzi",
            },
            [TemplateOf(Slot(false, Prefix("ka")), Slot(true, Infix("z")))] = new[] { "kzasag", "kasag" },
            [TemplateOf(Slot(false, Suffix("ti")), Slot(true, RepeatLast()))] = new[] { "sagtii", "sagti" },
            [TemplateOf(Slot(false, Suffix("ti")), Slot(true, Metathesis()))] = new[] { "sagit", "sagti" },
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
