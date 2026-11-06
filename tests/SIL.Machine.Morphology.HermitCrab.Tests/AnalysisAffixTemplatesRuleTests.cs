using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public class AnalysisAffixTemplatesRuleTests : HermitCrabTestBase
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void SharedWalkMatchesPerTemplateWalkInOrder(int variant)
    {
        var d = Suffix("d", "d");
        var dd = Suffix("dd", "dd");
        var s = Suffix("s", "s");
        AffixTemplateSlot Tail(bool optional = false) => new(d, dd) { Optional = optional };
        AffixTemplateSlot Inner() => new(s) { Optional = (variant & 1) == 0 };
        AffixTemplate[] templates =
        [
            Template(Inner(), Tail()),
            Template(Tail()),
            Template(Inner(), Tail(true)),
            Template(new AffixTemplateSlot(d) { Optional = true }, Inner(), Tail()),
            Template(new AffixTemplateSlot(s), Tail()),
            Template(),
            Template(Inner(), Tail()),
            Template(new AffixTemplateSlot(dd, d)),
            Template(new AffixTemplateSlot()),
        ];
        templates[4].RequiredSyntacticFeatureStruct = Pos("N");
        templates[3].IsFinal = false;
        if ((variant & 2) != 0)
            Array.Reverse(templates);

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1)
        {
            RuleSelector = rule => variant != 3 || !ReferenceEquals(rule, templates[1]),
        };
        IRule<Word, ShapeNode>[] rules = templates.Select(t => t.CompileAnalysisRule(morpher)).ToArray();
        var shared = new AnalysisAffixTemplatesRule(morpher, rules);
        var own = new RuleBatch<Word, ShapeNode>(rules, false, FreezableEqualityComparer<Word>.Default);

        int compared = 0;
        foreach (string text in Inputs(4))
        {
            foreach (FeatureStruct fs in new[] { FeatureStruct.New().Value, Pos("N"), Pos("V") })
            {
                foreach (
                    FinalTemplateState finalState in new[] { FinalTemplateState.None, FinalTemplateState.NonTemplate }
                )
                {
                    Word input = Input(text);
                    input.SyntacticFeatureStruct = fs;
                    input.FinalTemplateState = finalState;
                    input.Freeze();

                    Word[] expected = own.Apply(input).ToArray();
                    Word[] actual = shared.Apply(input).ToArray();
                    Assert.That(actual, Is.EqualTo(expected).Using(FreezableEqualityComparer<Word>.Default), text);
                    Assert.That(
                        actual.Select(w => new AnalysisStratumRule.AnalysisMergeKey(w)),
                        Is.EqualTo(expected.Select(w => new AnalysisStratumRule.AnalysisMergeKey(w))),
                        text
                    );
                    compared += actual.Length;
                }
            }
        }
        Assert.That(compared, Is.GreaterThan(1000));
    }

    [Test]
    public void UntracedBatteryRunsSharedSlotBatchOnce()
    {
        var d = Suffix("d", "d");
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        IRule<Word, ShapeNode>[] rules = new[]
        {
            Template(new AffixTemplateSlot(d)),
            Template(new AffixTemplateSlot(d)),
        }
            .Select(t => t.CompileAnalysisRule(morpher))
            .ToArray();
        var own = new RuleBatch<Word, ShapeNode>(rules, false, FreezableEqualityComparer<Word>.Default);
        var shared = new AnalysisAffixTemplatesRule(morpher, rules);
        Word input = Input("sagd");
        input.Freeze();

        Word[] expected = own.Apply(input).ToArray();
        Assert.That(d.ApplyCount, Is.EqualTo(2));
        d.ApplyCount = 0;
        Assert.That(shared.Apply(input), Is.EqualTo(expected).Using(FreezableEqualityComparer<Word>.Default));
        Assert.That(d.ApplyCount, Is.EqualTo(1), "two identical tails must execute one slot batch");
        _ = shared.Apply(input).ToArray();
        Assert.That(d.ApplyCount, Is.EqualTo(2), "each battery call must execute its own batch");
    }

    [TestCase(false, 2)]
    [TestCase(true, 1)]
    public void TracedAndParallelBatteriesKeepPerTemplateWalk(bool tracing, int degree)
    {
        var d = Suffix("d", "d");
        var traceManager = new TraceManager { IsTracing = tracing };
        var morpher = new Morpher(traceManager, Language, maxDegreeOfParallelism: degree);
        var shared = new AnalysisAffixTemplatesRule(
            morpher,
            new[] { Template(new AffixTemplateSlot(d)), Template(new AffixTemplateSlot(d)) }.Select(t =>
                t.CompileAnalysisRule(morpher)
            )
        );
        Word input = Input("sagd");
        input.Freeze();
        if (tracing)
            traceManager.AnalyzeWord(Language, input);
        _ = shared.Apply(input).ToArray();
        Assert.That(d.ApplyCount, Is.EqualTo(2));
    }

    [Test]
    public void SharedWalkPreservesBatteryAlternativeLimit()
    {
        var d = Suffix("d", "d");
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        IRule<Word, ShapeNode>[] rules = new[]
        {
            Template(new AffixTemplateSlot(d) { Optional = true }),
            Template(new AffixTemplateSlot(d) { Optional = true }),
        }
            .Select(t => t.CompileAnalysisRule(morpher))
            .ToArray();
        var shared = new AnalysisAffixTemplatesRule(morpher, rules) { MaxAlternatives = 1 };
        var own = new RuleBatch<Word, ShapeNode>(rules, false, FreezableEqualityComparer<Word>.Default)
        {
            MaxAlternatives = 1,
        };
        Word input = Input("sagd");
        input.Freeze();
        Assert.Throws<MaxAlternativesExceededException>(() => own.Apply(input).ToArray());
        Assert.Throws<MaxAlternativesExceededException>(() => shared.Apply(input).ToArray());
    }

    [Test]
    public void AlternativeLimitStopsBeforeLaterTemplateAdmission()
    {
        var d = Suffix("d", "d");
        AffixTemplate[] templates =
        [
            Template(new AffixTemplateSlot(d) { Optional = true }),
            Template(new AffixTemplateSlot(d) { Optional = true }),
            Template(new AffixTemplateSlot(Suffix("s", "s"))),
        ];
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1)
        {
            RuleSelector = rule =>
                !ReferenceEquals(rule, templates[2])
                    ? true
                    : throw new InvalidOperationException("later template must not be reached"),
        };
        IRule<Word, ShapeNode>[] rules = templates.Select(t => t.CompileAnalysisRule(morpher)).ToArray();
        var own = new RuleBatch<Word, ShapeNode>(rules, false, FreezableEqualityComparer<Word>.Default)
        {
            MaxAlternatives = 1,
        };
        var shared = new AnalysisAffixTemplatesRule(morpher, rules) { MaxAlternatives = 1 };
        Word input = Input("sagd");
        input.Freeze();

        Assert.Throws<MaxAlternativesExceededException>(() => own.Apply(input).ToArray());
        Assert.Throws<MaxAlternativesExceededException>(() => shared.Apply(input).ToArray());
    }

    [TestCase(false, 1)]
    [TestCase(true, 0)]
    public void ChangedSlotOptionalityPreservesPerTemplateWalk(bool optional, int changedTemplate)
    {
        var d = Suffix("d", "d");
        AffixTemplate[] templates =
        [
            Template(new AffixTemplateSlot(d) { Optional = optional }),
            Template(new AffixTemplateSlot(d) { Optional = optional }),
        ];
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        IRule<Word, ShapeNode>[] rules = templates.Select(t => t.CompileAnalysisRule(morpher)).ToArray();
        var own = new RuleBatch<Word, ShapeNode>(rules, false, FreezableEqualityComparer<Word>.Default);
        var shared = new AnalysisAffixTemplatesRule(morpher, rules);
        templates[changedTemplate].Slots[0].Optional = !optional;
        Word input = Input("sag");
        input.Freeze();

        Assert.That(shared.Apply(input), Is.EqualTo(own.Apply(input)).Using(FreezableEqualityComparer<Word>.Default));
    }

    private sealed record LookaheadRun(string[] Output, int InnerA, int InnerS, int Tail, List<string> Shadow);

    // Two templates share the tail slot "ad". Template A needs an inner "a" suffix, which the tail's unapplication
    // consumes from "sad"; template B has an optional inner "s" slot and still yields its outputs.
    private LookaheadRun RunSharedTail(RequiredSlotLookaheadMode mode, string text, bool innerAIsOptional = false)
    {
        CountingSuffix a = Suffix("a", "a");
        CountingSuffix s = Suffix("s", "s");
        CountingSuffix tail = Suffix("ad", "ad");
        var shadow = new List<string>();
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1)
        {
            RequiredSlotLookahead = mode,
            RequiredSlotShadow = (kind, template, slot) => shadow.Add(kind),
        };
        IRule<Word, ShapeNode>[] rules = new[]
        {
            Template(new AffixTemplateSlot(a) { Optional = innerAIsOptional }, new AffixTemplateSlot(tail)),
            Template(new AffixTemplateSlot(s) { Optional = true }, new AffixTemplateSlot(tail)),
        }
            .Select(t => t.CompileAnalysisRule(morpher))
            .ToArray();
        var shared = new AnalysisAffixTemplatesRule(morpher, rules);
        Word input = Input(text);
        input.Freeze();
        string[] output = shared.Apply(input).Select(Signature).ToArray();
        return new LookaheadRun(output, a.ApplyCount, s.ApplyCount, tail.ApplyCount, shadow);
    }

    [Test]
    public void LookaheadPrunesOnlyTheTemplateWhoseRequiredSlotIsGone()
    {
        LookaheadRun off = RunSharedTail(RequiredSlotLookaheadMode.Off, "sad");
        LookaheadRun on = RunSharedTail(RequiredSlotLookaheadMode.On, "sad");

        Assert.That(off.InnerA, Is.EqualTo(1));
        Assert.That(on.InnerA, Is.EqualTo(0), "the required inner suffix cannot unapply once the tail took the a");
        Assert.That(on.Tail, Is.EqualTo(off.Tail), "the shared tail still runs once");
        Assert.That(on.InnerS, Is.EqualTo(off.InnerS), "the other template is unaffected");
        Assert.That(off.Output, Is.Not.Empty);
        Assert.That(on.Output, Is.EqualTo(off.Output));
    }

    [Test]
    public void LookaheadKeepsEveryAttemptWhenBothTemplatesAreFeasible()
    {
        LookaheadRun off = RunSharedTail(RequiredSlotLookaheadMode.Off, "saad");
        LookaheadRun on = RunSharedTail(RequiredSlotLookaheadMode.On, "saad");

        Assert.That((on.InnerA, on.InnerS, on.Tail), Is.EqualTo((off.InnerA, off.InnerS, off.Tail)));
        Assert.That(on.Output, Is.EqualTo(off.Output));
    }

    [TestCase("sad", false)]
    [TestCase("saad", false)]
    [TestCase("sad", true)]
    [TestCase("dd", false)]
    public void ShadowLookaheadOnTheSharedWalkNeverViolatesOrChangesTheOutput(string text, bool optional)
    {
        LookaheadRun off = RunSharedTail(RequiredSlotLookaheadMode.Off, text, optional);
        LookaheadRun shadow = RunSharedTail(RequiredSlotLookaheadMode.Shadow, text, optional);

        Assert.That(shadow.Output, Is.EqualTo(off.Output));
        Assert.That((shadow.InnerA, shadow.InnerS, shadow.Tail), Is.EqualTo((off.InnerA, off.InnerS, off.Tail)));
        Assert.That(shadow.Shadow, Does.Not.Contain("violated"));
        Assert.That(shadow.Shadow, Does.Not.Contain("completed-under-fired"));
        if (text == "sad" && !optional)
            Assert.That(shadow.Shadow, Does.Contain("fired"));
    }

    [Test]
    public void LookaheadSharedWalkMatchesPerTemplateWalkOnEveryShortInput()
    {
        int compared = 0;
        foreach (string text in Inputs(5))
        {
            foreach (
                RequiredSlotLookaheadMode mode in new[] { RequiredSlotLookaheadMode.Off, RequiredSlotLookaheadMode.On }
            )
            {
                CountingSuffix a = Suffix("a", "a");
                CountingSuffix s = Suffix("s", "s");
                CountingSuffix tail = Suffix("ad", "ad");
                var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1)
                {
                    RequiredSlotLookahead = mode,
                };
                IRule<Word, ShapeNode>[] rules = new[]
                {
                    Template(new AffixTemplateSlot(a), new AffixTemplateSlot(tail)),
                    Template(
                        new AffixTemplateSlot(s) { Optional = true },
                        new AffixTemplateSlot(a),
                        new AffixTemplateSlot(tail)
                    ),
                    Template(new AffixTemplateSlot(s), new AffixTemplateSlot(tail)),
                }
                    .Select(t => t.CompileAnalysisRule(morpher))
                    .ToArray();
                var own = new RuleBatch<Word, ShapeNode>(rules, false, FreezableEqualityComparer<Word>.Default);
                var shared = new AnalysisAffixTemplatesRule(morpher, rules);
                Word input = Input(text);
                input.Freeze();
                Word[] expected = own.Apply(input).ToArray();
                Word[] actual = shared.Apply(input).ToArray();
                Assert.That(actual, Is.EqualTo(expected).Using(FreezableEqualityComparer<Word>.Default), text);
                compared += actual.Length;
            }
        }
        Assert.That(compared, Is.GreaterThan(10));
    }

    private static string Signature(Word w) => w + " " + string.Join(",", w.MorphologicalRules.Select(r => r.Name));

    private Word Input(string text) => new(Morphophonemic, Table3.Segment(text));

    private FeatureStruct Pos(string symbol) => FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol(symbol).Value;

    private static AffixTemplate Template(params AffixTemplateSlot[] slots)
    {
        var template = new AffixTemplate();
        foreach (AffixTemplateSlot slot in slots)
            template.Slots.Add(slot);
        return template;
    }

    private CountingSuffix Suffix(string name, string suffix)
    {
        var rule = new CountingSuffix { Name = name, MaxApplicationCount = 3 };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>
                        .New("stem")
                        .Annotation(FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value)
                        .OneOrMore.Value,
                },
                Rhs = { new CopyFromInput("stem"), new InsertSegments(Table3, suffix) },
            }
        );
        return rule;
    }

    private static IEnumerable<string> Inputs(int maxLength)
    {
        string[] frontier = [""];
        yield return "";
        for (int length = 1; length <= maxLength; length++)
        {
            frontier = frontier.SelectMany(w => new[] { "s", "a", "d" }.Select(c => w + c)).ToArray();
            foreach (string word in frontier)
                yield return word;
        }
    }

    private sealed class CountingSuffix : AffixProcessRule
    {
        public int ApplyCount;

        public override IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher) =>
            new CountingRule(this, base.CompileAnalysisRule(morpher));

        private sealed class CountingRule(CountingSuffix owner, IRule<Word, ShapeNode> rule) : IRule<Word, ShapeNode>
        {
            public IEnumerable<Word> Apply(Word input)
            {
                Interlocked.Increment(ref owner.ApplyCount);
                return rule.Apply(input);
            }
        }
    }
}
