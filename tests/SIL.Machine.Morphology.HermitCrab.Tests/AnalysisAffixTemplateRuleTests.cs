using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab;

// A template's required syntactic features gate unapplication but are never added to the output words.
// Word.ValueEquals omits SyntacticFeatureStruct because analysis derives it from the rule trail alone,
// and adding a template's features would break that: a template can be passed through with every slot
// left empty, leaving the shape and the trail untouched.
// AffixTemplateTests.SameRuleUsedInMultipleTemplates covers the end-to-end effect.
[TestFixture]
public class AnalysisAffixTemplateRuleTests : HermitCrabTestBase
{
    private static readonly FeatureStruct Any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    [Test]
    public void Apply_SlotLeftEmpty_DoesNotAddTheTemplateFeaturesToTheOutput()
    {
        FeatureStruct v = Pos("V");
        AffixTemplate template = VerbTemplate(v);

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        IRule<Word, ShapeNode> templateRule = template.CompileAnalysisRule(morpher);

        var input = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment("sags"));
        input.Freeze();

        Word outWord = templateRule.Apply(input).Single();

        Assert.That(
            outWord.SyntacticFeatureStruct.ValueEquals(input.SyntacticFeatureStruct),
            Is.True,
            "a pass through the template with its only slot left empty must leave SyntacticFeatureStruct alone"
        );
    }

    // The same suffix unapplied with and without a template in front of it reaches Word.ValueEquals-equal
    // words, so differing feature structures would drop one of them wherever Words are deduplicated.
    [Test]
    public void Apply_SlotLeftEmpty_LeavesTheSameStateAsNotEnteringTheTemplate()
    {
        FeatureStruct v = Pos("V");
        AffixTemplate template = VerbTemplate(v);
        AffixProcessRule sSuffix = Suffix("s_suffix", FeatureStruct.New().Value, v, Table3, "+s");

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        IRule<Word, ShapeNode> templateRule = template.CompileAnalysisRule(morpher);
        var sRule = new AnalysisAffixProcessRule(morpher, sSuffix);

        var input = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment("sags"));
        input.Freeze();

        Word viaTemplate = sRule.Apply(templateRule.Apply(input).Single()).Single();
        Word direct = sRule.Apply(input).Single();

        Assert.Multiple(() =>
        {
            Assert.That(FreezableEqualityComparer<Word>.Default.Equals(viaTemplate, direct), Is.True);
            Assert.That(
                viaTemplate.SyntacticFeatureStruct.ValueEquals(direct.SyntacticFeatureStruct),
                Is.True,
                "Word.ValueEquals-equal analyses must agree on SyntacticFeatureStruct"
            );
        });
    }

    // Slots are listed in synthesis order and unapplied from the last to the first, so slot 0 is the suffix
    // closest to the stem and a word ends with the material of its highest slot.
    private static AffixTemplateSlot Slot(bool optional, params MorphemicMorphologicalRule[] rules) =>
        new(rules) { Optional = optional };

    private static AffixTemplate TemplateOf(params AffixTemplateSlot[] slots)
    {
        var template = new AffixTemplate { Name = "lookahead_template" };
        foreach (AffixTemplateSlot slot in slots)
            template.Slots.Add(slot);
        return template;
    }

    // The analysis of a rule that deletes a final segment in synthesis: it adds the segment back, so the
    // unapplication produces a shape that is not made of copies of the input.
    private AffixProcessRule SegmentRestoringRule(string name, string restored)
    {
        var rule = new AffixProcessRule
        {
            Id = name,
            Name = name,
            Gloss = name,
            RequiredSyntacticFeatureStruct = FeatureStruct.New().Value,
            OutSyntacticFeatureStruct = FeatureStruct.New().Value,
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(Character(Table3, restored)).Value,
                },
                Rhs = { new CopyFromInput("1") },
            }
        );
        return rule;
    }

    private AffixProcessRule SuffixRule(string name, string insert) =>
        Suffix(name, FeatureStruct.New().Value, FeatureStruct.New().Value, Table3, insert);

    private Word Input(string form, bool lastNodeOptional = false)
    {
        var input = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment(form));
        if (lastNodeOptional)
            input.Shape.Last.Annotation.Optional = true;
        return input;
    }

    private sealed record WalkResult(List<string> Parses, int RuleAttempts);

    private WalkResult Walk(
        AffixTemplate template,
        Word input,
        RequiredSlotLookaheadMode mode,
        int maxDegreeOfParallelism = 1,
        bool tracing = false
    )
    {
        int attempts = 0;
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism)
        {
            RequiredSlotLookahead = mode,
            RuleSelector = rule =>
            {
                if (rule is AffixProcessRule)
                    System.Threading.Interlocked.Increment(ref attempts);
                return true;
            },
        };
        TraceManager.IsTracing = tracing;
        if (tracing)
            TraceManager.AnalyzeWord(Language, input);
        input.Freeze();
        IRule<Word, ShapeNode> rule = template.CompileAnalysisRule(morpher);
        List<string> parses = rule.Apply(input)
            .Select(w => w.ToString() + " " + string.Join(",", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Gloss)))
            .OrderBy(p => p, System.StringComparer.Ordinal)
            .ToList();
        return new WalkResult(parses, attempts);
    }

    private (WalkResult Off, WalkResult On) WalkBoth(AffixTemplate template, string form, bool lastNodeOptional = false)
    {
        WalkResult off = Walk(template, Input(form, lastNodeOptional), RequiredSlotLookaheadMode.Off);
        WalkResult on = Walk(template, Input(form, lastNodeOptional), RequiredSlotLookaheadMode.On);
        Assert.That(on.Parses, Is.EqualTo(off.Parses), "the look-ahead must not change the parses");
        return (off, on);
    }

    // stem + required -ti + optional -u/-f/-v + optional -z
    private AffixTemplate RequiredInnerSuffixTemplate() =>
        TemplateOf(
            Slot(false, SuffixRule("ti", "ti")),
            Slot(true, SuffixRule("u", "u"), SuffixRule("f", "f"), SuffixRule("v", "v")),
            Slot(true, SuffixRule("z", "z"))
        );

    [Test]
    public void Apply_RequiredSlotLiteralAbsent_PrunesBeforeTryingTheOptionalSlots()
    {
        AffixTemplate template = RequiredInnerSuffixTemplate();

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagufz");

        Assert.Multiple(() =>
        {
            Assert.That(off.Parses, Is.Empty);
            Assert.That(off.RuleAttempts, Is.GreaterThan(0));
            Assert.That(on.RuleAttempts, Is.EqualTo(0), "no rule can supply -ti, so no slot rule is attempted");
        });
    }

    [Test]
    public void Apply_RequiredSlotLiteralPresent_DoesNotPrune()
    {
        AffixTemplate template = RequiredInnerSuffixTemplate();

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagtiuz");

        Assert.Multiple(() =>
        {
            Assert.That(off.Parses, Is.Not.Empty);
            Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts));
        });
    }

    [Test]
    public void Apply_RequiredSlotLiteralLostAfterAnOptionalSlot_PrunesTheInnerPartial()
    {
        AffixTemplate template = RequiredInnerSuffixTemplate();

        // -z unapplies, which leaves "sagtuu"; no node can still be the i of -ti
        (WalkResult off, WalkResult on) = WalkBoth(template, "sagttz");

        Assert.Multiple(() =>
        {
            Assert.That(off.Parses, Is.Empty);
            Assert.That(on.RuleAttempts, Is.LessThan(off.RuleAttempts));
        });
    }

    [Test]
    public void Apply_ImpureSlotBetween_DoesNotPruneForTheInnerRequiredSlot()
    {
        // the middle slot restores the i that -ti needs, so "sagt" is a stem plus -ti once it has run
        AffixTemplate template = TemplateOf(
            Slot(false, SuffixRule("ti", "ti")),
            Slot(true, SegmentRestoringRule("restore_i", "i"))
        );

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagt");

        Assert.That(on.Parses, Is.Not.Empty, "the parse that needs the restored segment must survive");
        Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts));
    }

    [Test]
    public void Apply_SeveralRequiredSlots_PrunesWhicheverLiteralIsAbsent()
    {
        // -u is required as well as -ti; the word has neither
        AffixTemplate template = TemplateOf(
            Slot(false, SuffixRule("ti", "ti")),
            Slot(true, SuffixRule("z", "z")),
            Slot(false, SuffixRule("u", "u"))
        );

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagfz");

        Assert.That(on.RuleAttempts, Is.EqualTo(0));
        Assert.That(off.RuleAttempts, Is.GreaterThan(0));
    }

    [Test]
    public void Apply_OptionalNodeThatUnifiesWithTheLiteral_CountsAsPresent()
    {
        AffixTemplate template = TemplateOf(Slot(false, SuffixRule("ti", "ti")));

        // "sagti" where the i stands for a deleted segment: the matcher may match or skip an optional node
        (WalkResult off, WalkResult on) = WalkBoth(template, "sagti", lastNodeOptional: true);

        Assert.Multiple(() =>
        {
            Assert.That(off.Parses, Is.Not.Empty);
            Assert.That(on.Parses, Is.EqualTo(off.Parses));
        });
    }

    [Test]
    public void Apply_RequiredSlotWithNullAffix_IsNeverPruned()
    {
        AffixProcessRule withNull = SuffixRule("ti_or_null", "ti");
        withNull.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") },
            }
        );
        AffixTemplate template = TemplateOf(Slot(false, withNull), Slot(true, SuffixRule("z", "z")));

        (WalkResult off, WalkResult on) = WalkBoth(template, "sagz");

        Assert.Multiple(() =>
        {
            Assert.That(on.Parses, Is.Not.Empty);
            Assert.That(on.RuleAttempts, Is.EqualTo(off.RuleAttempts));
        });
    }

    // The input lacks the inner slot's "ti"; a pure outer rule permits pruning before it is tried.
    private (WalkResult Off, WalkResult On) WalkWithOptionalSlot(
        Pattern<Word, ShapeNode>[] lhs,
        MorphologicalOutputAction[] rhs
    )
    {
        var rule = new AffixProcessRule
        {
            Id = "z_slot",
            Name = "z_slot",
            Gloss = "z_slot",
            RequiredSyntacticFeatureStruct = FeatureStruct.New().Value,
            OutSyntacticFeatureStruct = FeatureStruct.New().Value,
        };
        var allomorph = new AffixProcessAllomorph();
        foreach (Pattern<Word, ShapeNode> part in lhs)
            allomorph.Lhs.Add(part);
        foreach (MorphologicalOutputAction action in rhs)
            allomorph.Rhs.Add(action);
        allomorph.Rhs.Add(new InsertSegments(Table3, "z"));
        rule.Allomorphs.Add(allomorph);
        AffixTemplate template = TemplateOf(Slot(false, SuffixRule("ti", "ti")), Slot(true, rule));
        return WalkBoth(template, "sagz");
    }

    [Test]
    public void Apply_CopiedPartCappedAtOneThatMayMatchNothing_MakesTheSlotImpure()
    {
        (WalkResult off, WalkResult on) = WalkWithOptionalSlot(
            new[] { Pattern<Word, ShapeNode>.New("1").Annotation(Any).Optional.Value },
            new MorphologicalOutputAction[] { new CopyFromInput("1") }
        );

        Assert.That(on.RuleAttempts, Is.GreaterThan(0), "the optional rule is still tried at entry");
    }

    [Test]
    public void Apply_ModifyFromInputInASlot_MakesTheSlotImpure()
    {
        FeatureStruct voiced = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("vd+")
            .Value;

        (WalkResult off, WalkResult on) = WalkWithOptionalSlot(
            new[] { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
            new MorphologicalOutputAction[]
            {
                new ModifyFromInput("1", voiced, System.Array.Empty<SymbolicFeatureValue>()),
            }
        );

        Assert.That(on.RuleAttempts, Is.GreaterThan(0), "the optional rule is still tried at entry");
    }

    [Test]
    public void Apply_CopiedAlternationOfSegments_KeepsTheSlotPure()
    {
        (WalkResult off, WalkResult on) = WalkWithOptionalSlot(
            new[]
            {
                Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                Pattern<Word, ShapeNode>
                    .New("2")
                    .Annotation(Character(Table3, "i"))
                    .Or.Annotation(Character(Table3, "u"))
                    .Value,
            },
            new MorphologicalOutputAction[] { new CopyFromInput("1"), new CopyFromInput("2") }
        );

        Assert.That(off.RuleAttempts, Is.GreaterThan(0));
        Assert.That(on.RuleAttempts, Is.EqualTo(0));
    }

    [Test]
    public void Apply_CopiedAlternationWithAnOptionalBranch_MakesTheSlotImpure()
    {
        (WalkResult off, WalkResult on) = WalkWithOptionalSlot(
            new[]
            {
                Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                Pattern<Word, ShapeNode>
                    .New("2")
                    .Annotation(Character(Table3, "i"))
                    .Optional.Or.Annotation(Character(Table3, "u"))
                    .Value,
            },
            new MorphologicalOutputAction[] { new CopyFromInput("1"), new CopyFromInput("2") }
        );

        Assert.That(on.RuleAttempts, Is.GreaterThan(0), "the optional rule is still tried at entry");
    }

    [Test]
    public void Apply_SlotOptionalityChangedAfterCompiling_IsReadLive()
    {
        AffixTemplate template = RequiredInnerSuffixTemplate();
        var morpher = new Morpher(TraceManager, Language, 1) { RequiredSlotLookahead = RequiredSlotLookaheadMode.On };
        IRule<Word, ShapeNode> rule = template.CompileAnalysisRule(morpher);
        Word input = Input("sagu");
        input.Freeze();

        Assert.That(rule.Apply(input), Is.Empty, "slot 0 is required and -ti is absent");

        template.Slots[0].Optional = true;
        Assert.That(rule.Apply(input), Is.Not.Empty, "an optional slot is not tested");

        template.Slots[0].Optional = false;
        Assert.That(rule.Apply(input), Is.Empty);
    }

    [TestCase(0)]
    [TestCase(4)]
    public void Apply_Parallel_ReturnsTheSameParsesAsSequential(int maxDegreeOfParallelism)
    {
        AffixTemplate template = RequiredInnerSuffixTemplate();

        foreach (string form in new[] { "sagtiuz", "sagufz", "sagttz", "sagtif" })
        {
            WalkResult sequential = Walk(template, Input(form), RequiredSlotLookaheadMode.On);
            WalkResult parallel = Walk(template, Input(form), RequiredSlotLookaheadMode.On, maxDegreeOfParallelism);
            WalkResult parallelOff = Walk(template, Input(form), RequiredSlotLookaheadMode.Off, maxDegreeOfParallelism);
            Assert.That(parallel.Parses, Is.EqualTo(sequential.Parses), form);
            Assert.That(parallel.Parses, Is.EqualTo(parallelOff.Parses), form);
        }
    }

    [Test]
    public void Apply_Tracing_RunsTheRulesForDetailedFailureInformation()
    {
        AffixTemplate template = RequiredInnerSuffixTemplate();
        Word input = Input("sagufz");

        WalkResult result = Walk(template, input, RequiredSlotLookaheadMode.On, tracing: true);
        TraceManager.IsTracing = false;

        Assert.That(result.RuleAttempts, Is.GreaterThan(0));
        var root = (Trace)input.CurrentTrace;
        Assert.That(root.Children.Count, Is.GreaterThan(2));
    }

    [Test]
    public void Apply_PublicPruningSwitch_ControlsRuleAttempts()
    {
        AffixTemplate template = RequiredInnerSuffixTemplate();
        int attempts = 0;
        var morpher = new Morpher(TraceManager, Language, 1)
        {
            RuleSelector = rule =>
            {
                if (rule is AffixProcessRule)
                    attempts++;
                return true;
            },
        };
        Word input = Input("sagufz");
        input.Freeze();
        var rule = template.CompileAnalysisRule(morpher);
        Assert.That(morpher.PruneRequiredSlots, Is.True);
        Assert.That(rule.Apply(input), Is.Empty);
        Assert.That(attempts, Is.EqualTo(0));
        morpher.PruneRequiredSlots = false;
        Assert.That(rule.Apply(input), Is.Empty);
        Assert.That(attempts, Is.GreaterThan(0));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Compile_UntypedConstraints_DisablesTheSummaryConservatively(bool untypedLhs)
    {
        var fs = FeatureStruct.New().Value;
        fs.Freeze();
        var rule = SuffixRule("untyped", "z");
        if (untypedLhs)
        {
            rule.Allomorphs[0].Lhs.Add(Pattern<Word, ShapeNode>.New("2").Annotation(fs).Value);
        }
        else
        {
            rule.Allomorphs[0]
                .Rhs.Add(new InsertSimpleContext(new NaturalClass(fs), System.Array.Empty<SymbolicFeatureValue>()));
        }
        var summary = new RequiredSlotMaterial(TemplateOf(Slot(false, rule)));
        var walk = SlotWalk.Start();
        long unifications = 0;
        Assert.That(summary.IsPure(0), Is.False);
        Assert.That(summary.CannotUnapply(0, Input("sag").Shape, ref walk, ref unifications), Is.False);
    }

    [TestCase(1)]
    [TestCase(0)]
    public void Apply_Shadow_ReportsFiredPointsAndNeverViolatesOrChangesTheParses(int maxDegreeOfParallelism)
    {
        AffixTemplate template = RequiredInnerSuffixTemplate();
        var events = new List<string>();

        foreach (string form in new[] { "sagtiuz", "sagufz", "sagttz", "sagtif", "sagu" })
        {
            WalkResult off = Walk(template, Input(form), RequiredSlotLookaheadMode.Off, maxDegreeOfParallelism);
            Word input = Input(form);
            input.Freeze();
            var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism)
            {
                RequiredSlotLookahead = RequiredSlotLookaheadMode.Shadow,
                RequiredSlotShadow = (kind, t, slot) =>
                {
                    lock (events)
                        events.Add(kind);
                },
            };
            TraceManager.IsTracing = false;
            string[] parses = template
                .CompileAnalysisRule(morpher)
                .Apply(input)
                .Select(w =>
                    w.ToString() + " " + string.Join(",", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Gloss))
                )
                .OrderBy(p => p, System.StringComparer.Ordinal)
                .ToArray();
            Assert.That(parses, Is.EqualTo(off.Parses), form);
        }

        Assert.Multiple(() =>
        {
            Assert.That(events.Count(e => e == "fired"), Is.GreaterThan(0));
            Assert.That(events.Count(e => e == "violated"), Is.EqualTo(0));
            Assert.That(events.Count(e => e == "completed-under-fired"), Is.EqualTo(0));
        });
    }

    private AffixTemplate VerbTemplate(FeatureStruct required)
    {
        var template = new AffixTemplate { Name = "verb_template", RequiredSyntacticFeatureStruct = required };
        template.Slots.Add(
            new AffixTemplateSlot(Suffix("d_suffix", required, required, Table3, "+d")) { Optional = true }
        );
        return template;
    }

    private static AffixProcessRule Suffix(
        string name,
        FeatureStruct required,
        FeatureStruct outFs,
        CharacterDefinitionTable table,
        string insert
    )
    {
        var rule = new AffixProcessRule
        {
            Id = name,
            Name = name,
            Gloss = name,
            RequiredSyntacticFeatureStruct = required,
            OutSyntacticFeatureStruct = outFs,
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(table, insert) },
            }
        );
        return rule;
    }

    private FeatureStruct Pos(string symbol) => FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol(symbol).Value;
}
