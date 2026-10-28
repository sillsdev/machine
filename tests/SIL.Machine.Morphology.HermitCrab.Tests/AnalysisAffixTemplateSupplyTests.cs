using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

// A truncating slot writes a segment back when it unapplies. Slots behind it may be missing that many
// segments, if it can write them. Slots are in synthesis order and unapplied last to first.
[TestFixture]
public class AnalysisAffixTemplateSupplyTests : HermitCrabTestBase
{
    private static readonly FeatureStruct Any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    private sealed record WalkResult(List<string> Parses, int RuleAttempts, int Violations);

    private FeatureStruct Vowel =>
        FeatureStruct.New(Language.PhonologicalFeatureSystem).Symbol(HCFeatureSystem.Segment).Symbol("voc+").Value;

    private static AffixTemplateSlot Slot(bool optional, params MorphemicMorphologicalRule[] rules) =>
        new(rules) { Optional = optional };

    private static AffixTemplate TemplateOf(params AffixTemplateSlot[] slots)
    {
        var template = new AffixTemplate { Name = "supply_template" };
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

    // Deletes the last segment of the stem, which has to match deleted, and then adds the suffix.
    private AffixProcessRule TruncatingSuffix(string insert, FeatureStruct deleted) =>
        RuleOf(
            "truncating_" + insert,
            new[]
            {
                Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                Pattern<Word, ShapeNode>.New("2").Annotation(deleted).Value,
            },
            new CopyFromInput("1"),
            new InsertSegments(Table3, insert)
        );

    // Deletes the last two segments of the stem, so unapplying it writes two segments back.
    private AffixProcessRule TwoSegmentTruncatingSuffix(string insert) =>
        RuleOf(
            "truncating2_" + insert,
            new[]
            {
                Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                Pattern<Word, ShapeNode>.New("2").Annotation(Any).Value,
                Pattern<Word, ShapeNode>.New("3").Annotation(Any).Value,
            },
            new CopyFromInput("1"),
            new InsertSegments(Table3, insert)
        );

    // An uncopied run of up to two segments that may be empty, which is written back as two optional segments.
    private AffixProcessRule RunOfTwoSuffix(string insert) =>
        RuleOf(
            "run2_" + insert,
            new[]
            {
                Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                Pattern<Word, ShapeNode>.New("2").Annotation(Any).Range(0, 2).Value,
            },
            new CopyFromInput("1"),
            new InsertSegments(Table3, insert)
        );

    private WalkResult Walk(AffixTemplate template, string form, RequiredSlotLookaheadMode mode)
    {
        int attempts = 0;
        int violations = 0;
        var morpher = new Morpher(TraceManager, Language, 1)
        {
            RequiredSlotLookahead = mode,
            RuleSelector = rule =>
            {
                if (rule is AffixProcessRule)
                    attempts++;
                return true;
            },
            RequiredSlotShadow = (kind, t, slot) =>
            {
                if (kind != "fired")
                    violations++;
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
    public void Apply_RequiredSlotNeedsMoreSegmentsThanTheTruncatingSlotRestores_PrunesAtEntry()
    {
        // -ti needs two segments, the shape has neither, and one deleted segment gives back at most one
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, TruncatingSuffix("z", Any)));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagz");

        Assert.Multiple(() =>
        {
            Assert.That(off.RuleAttempts, Is.GreaterThan(0));
            Assert.That(on.RuleAttempts, Is.EqualTo(0));
        });
    }

    [Test]
    public void Apply_RequiredSlotNeedsOneSegmentTheTruncatingSlotRestores_KeepsTheParse()
    {
        // sag + ti, then the i is deleted and z added: the i of -ti is not in the word
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, TruncatingSuffix("z", Any)));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagtz");

        Assert.Multiple(() =>
        {
            Assert.That(on.Parses, Is.Not.Empty);
            Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts));
        });
    }

    [Test]
    public void Apply_MissingSegmentIsOneTheTruncatingSlotCannotRestore_PrunesAtEntry()
    {
        // the deleted segment is a vowel, so it cannot be the t of -t
        AffixTemplate template = TemplateOf(Slot(false, Suffix("t")), Slot(true, TruncatingSuffix("z", Vowel)));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagz");

        Assert.Multiple(() =>
        {
            Assert.That(off.RuleAttempts, Is.GreaterThan(0));
            Assert.That(on.RuleAttempts, Is.EqualTo(0));
        });
    }

    [Test]
    public void Apply_MissingSegmentIsOneTheTruncatingSlotCanRestore_StillTriesTheRules()
    {
        AffixTemplate template = TemplateOf(Slot(false, Suffix("i")), Slot(true, TruncatingSuffix("z", Vowel)));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagz");

        Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts));
    }

    [Test]
    public void Apply_TwoTruncatingSlots_RestoreTheirSegmentsTogether()
    {
        // sag + ti, the i is deleted for z and the z for k: sagtk has neither t's partner i nor the z
        AffixTemplate template = TemplateOf(
            Slot(false, Suffix("ti")),
            Slot(true, TruncatingSuffix("z", Any)),
            Slot(true, TruncatingSuffix("k", Any))
        );

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagtk");
        Assert.Multiple(() =>
        {
            Assert.That(on.Parses, Is.Not.Empty);
            Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts));
        });

        (_, WalkResult onAbsent) = WalkBoth(template, "sagk");
        Assert.That(onAbsent.RuleAttempts, Is.GreaterThan(0), "two restored segments may be enough");
    }

    [Test]
    public void Apply_SlotThatWritesBackTwoSegments_KeepsAWordMissingTwoSegments()
    {
        // sag + ti, then both are deleted and z added: the t and i of -ti are not in the word
        AffixTemplate template = TemplateOf(Slot(false, Suffix("ti")), Slot(true, TwoSegmentTruncatingSuffix("z")));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagz");

        Assert.Multiple(() =>
        {
            Assert.That(on.Parses, Is.Not.Empty);
            Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts), "two restored segments are enough");
        });

        (WalkResult off3, WalkResult on3) = WalkBoth(
            TemplateOf(Slot(false, Suffix("tik")), Slot(true, TwoSegmentTruncatingSuffix("z"))),
            "sagz"
        );
        Assert.Multiple(() =>
        {
            Assert.That(off3.RuleAttempts, Is.GreaterThan(0));
            Assert.That(on3.RuleAttempts, Is.EqualTo(0), "three missing segments are more than two restored");
        });
    }

    [Test]
    public void Apply_SlotWithAnUncopiedRunOfUpToTwo_AllowsTwoMissingSegmentsAndNoMore()
    {
        (WalkResult off2, WalkResult on2) = WalkBoth(
            TemplateOf(Slot(false, Suffix("ti")), Slot(true, RunOfTwoSuffix("z"))),
            "sagz"
        );
        Assert.That(on2.RuleAttempts, Is.EqualTo(off2.RuleAttempts), "the run restores up to two segments");

        (WalkResult off3, WalkResult on3) = WalkBoth(
            TemplateOf(Slot(false, Suffix("tik")), Slot(true, RunOfTwoSuffix("z"))),
            "sagz"
        );
        Assert.Multiple(() =>
        {
            Assert.That(off3.RuleAttempts, Is.GreaterThan(0));
            Assert.That(on3.RuleAttempts, Is.EqualTo(0));
        });
    }

    [Test]
    public void Apply_OnEveryTruncationTemplateAndWord_KeepsTheParsesAndNeverViolatesTheShadow()
    {
        var templates = new Dictionary<AffixTemplate, string[]>
        {
            [TemplateOf(Slot(false, Suffix("ti")), Slot(true, TruncatingSuffix("z", Any)))] = new[]
            {
                "sagz",
                "sagtz",
                "sagtiz",
                "sagtzz",
            },
            [TemplateOf(Slot(false, Suffix("t")), Slot(true, TruncatingSuffix("z", Vowel)))] = new[]
            {
                "sagz",
                "sagtz",
                "sagtiz",
            },
            [
                TemplateOf(
                    Slot(false, Suffix("ti")),
                    Slot(true, TruncatingSuffix("z", Any)),
                    Slot(true, TruncatingSuffix("k", Any))
                )
            ] = new[] { "sagtk", "sagk", "sagtzk", "sagtik" },
            [
                TemplateOf(
                    Slot(false, TruncatingSuffix("u", Any)),
                    Slot(false, Suffix("ti")),
                    Slot(true, TruncatingSuffix("z", Any))
                )
            ] = new[] { "sagtu", "sagzu", "sagtiuz", "sagtzu" },
        };

        foreach ((AffixTemplate template, string[] forms) in templates)
        {
            foreach (string form in forms)
            {
                WalkResult off = Walk(template, form, RequiredSlotLookaheadMode.Off);
                WalkResult on = Walk(template, form, RequiredSlotLookaheadMode.On);
                WalkResult shadow = Walk(template, form, RequiredSlotLookaheadMode.Shadow);
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
