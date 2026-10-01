using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public class TraceReasonTests
{
    private Language _language = default!;
    private Stratum _stratum = default!;
    private CharacterDefinitionTable _table = default!;
    private TraceManager _traceManager = default!;
    private static readonly FeatureStruct Any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    [SetUp]
    public void SetUp()
    {
        var phonology = new FeatureSystem
        {
            new SymbolicFeature("letter", "abds".Select(c => new FeatureSymbol(c.ToString()))),
        };
        _table = new CharacterDefinitionTable();
        foreach (char c in "abds")
            _table.AddSegment(c.ToString(), FeatureStruct.New(phonology).Symbol(c.ToString()).Value.Clone());
        _table.AddBoundary("+");
        _stratum = new Stratum(_table) { Name = "lexical" };
        _language = new Language { PhonologicalFeatureSystem = phonology, Strata = { _stratum } };
        _language.SyntacticFeatureSystem.AddPartsOfSpeech(new FeatureSymbol("N"), new FeatureSymbol("V"));
        _traceManager = new TraceManager { IsTracing = true };
    }

    [TestCase("a", "Pattern")]
    [TestCase("as", "MaxApplicationCount")]
    public void AffixAnalysis_ReportsFailedGate(string form, string expectedReason)
    {
        AffixProcessRule rule = Suffix();
        Word input = AnalysisInput(form);
        if (expectedReason == "MaxApplicationCount")
            input.MorphologicalRuleUnapplied(rule);
        input.Freeze();
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Trace failure = Events(input).Single(t => t.Type == TraceType.MorphologicalRuleAnalysis);
        Assert.That(failure.FailureReason.ToString(), Is.EqualTo(expectedReason));
        Assert.That(failure.SubruleIndex, Is.EqualTo(expectedReason == "Pattern" ? 0 : -1));
    }

    [Test]
    public void AffixAnalysis_ReportsOutputFeatureConflict()
    {
        AffixProcessRule rule = Suffix();
        rule.OutSyntacticFeatureStruct = Pos("V");
        Word input = AnalysisInput("as");
        input.SyntacticFeatureStruct = Pos("N");
        input.Freeze();
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Trace failure = Events(input).Single(t => t.Type == TraceType.MorphologicalRuleAnalysis);
        Assert.That(failure.FailureReason.ToString(), Is.EqualTo("OutputSyntacticFeatureStruct"));
        Assert.That(failure.FailureObject, Is.InstanceOf<FeatureStruct>());
    }

    [Test]
    public void RealizationalAnalysis_ReportsFeatureConflict()
    {
        var rule = new RealizationalAffixProcessRule { RealizationalFeatureStruct = Pos("V") };
        rule.Allomorphs.Add(Suffix().Allomorphs[0]);
        Word input = AnalysisInput("as");
        input.RealizationalFeatureStruct = Pos("N");
        input.Freeze();
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Trace failure = Events(input).Single(t => t.Type == TraceType.MorphologicalRuleAnalysis);
        Assert.That(failure.FailureReason.ToString(), Is.EqualTo("RealizationalFeatureStruct"));
        Assert.That(failure.FailureObject, Is.SameAs(rule.RealizationalFeatureStruct));
    }

    [TestCase(1, true, "a", "OptionalSkipped")]
    [TestCase(2, true, "a", "OptionalSkipped")]
    [TestCase(1, false, "a", "RequiredUnfilled")]
    [TestCase(2, false, "a", "RequiredUnfilled")]
    [TestCase(1, false, "as", "RuleApplied")]
    [TestCase(2, false, "as", "RuleApplied")]
    public void TemplateAnalysis_ReportsSlotOutcome(int parallelism, bool optional, string form, string outcome)
    {
        var template = new AffixTemplate { Slots = { new AffixTemplateSlot(Suffix()) { Optional = optional } } };
        Word input = AnalysisInput(form);
        input.Freeze();
        Word[] output = template.CompileAnalysisRule(Morpher(parallelism)).Apply(input).ToArray();
        Assert.That(output.Length, Is.EqualTo(outcome == "RequiredUnfilled" ? 0 : 1));
        Trace slot = Events(input).Single(t => t.Type.ToString() == "TemplateSlotAnalysis");
        Assert.That(slot.SlotIndex, Is.EqualTo(0));
        Assert.That(slot.SlotOutcome, Is.EqualTo(Enum.Parse<TemplateSlotOutcome>(outcome)));
        Assert.That(slot.Output is null, Is.EqualTo(outcome != "RuleApplied"));
    }

    [TestCase(true, "OptionalSkipped")]
    [TestCase(false, "RequiredUnfilled")]
    public void TemplateSynthesis_ReportsEmptySlot(bool optional, string outcome)
    {
        var template = new AffixTemplate { Slots = { new AffixTemplateSlot(Suffix()) { Optional = optional } } };
        Word input = new Word(Entry("a").PrimaryAllomorph, new FeatureStruct());
        _traceManager.AnalyzeWord(_language, input);
        input.Freeze();
        Word[] output = template.CompileSynthesisRule(Morpher()).Apply(input).ToArray();
        Assert.That(output.Length, Is.EqualTo(optional ? 1 : 0));
        Trace slot = Events(input).Single(t => t.Type.ToString() == "TemplateSlotSynthesis");
        Assert.That(slot.SlotOutcome, Is.EqualTo(Enum.Parse<TemplateSlotOutcome>(outcome)));
    }

    [TestCase("a", 1)]
    [TestCase("b", 0)]
    public void LexicalLookup_ReportsCompletedCandidateCount(string form, int count)
    {
        Entry("a");
        Word[] output = Morpher().ParseWord(form, out object trace).ToArray();
        Assert.That(output.Length, Is.EqualTo(count));
        Trace lookup = Descendants((Trace)trace).Single(t => t.Type == TraceType.LexicalLookup);
        Assert.That(lookup.LexicalCandidateCount, Is.EqualTo(count));
        Assert.That(lookup.IsLexicalGuess, Is.EqualTo(false));
        Assert.That(lookup.Children.Count(t => t.Type == TraceType.WordSynthesis), Is.EqualTo(count));
    }

    [Test]
    public void Blocking_IdentifiesReplacementFamilyEntry()
    {
        LexEntry stem = Entry("a");
        LexEntry blocker = Entry("b");
        var family = new LexFamily { Entries = { stem, blocker } };
        _language.Families.Add(family);
        AffixProcessRule suffix = Suffix();
        suffix.Blockable = true;
        Word input = new Word(stem.PrimaryAllomorph, new FeatureStruct());
        input.MorphologicalRuleUnapplied(suffix);
        _traceManager.AnalyzeWord(_language, input);
        input.Freeze();
        Word result = suffix.CompileSynthesisRule(Morpher()).Apply(input).Single();
        Assert.That(result.RootAllomorph.Morpheme, Is.SameAs(blocker));
        Trace blocked = Events(input).Single(t => t.Type == TraceType.Blocked);
        Assert.That(blocked.BlockingEntry, Is.SameAs(blocker));
    }

    [Test]
    public void EnvironmentFailure_RetainsAllomorphAndFailedAlternatives()
    {
        RootAllomorph allomorph = Entry("a").PrimaryAllomorph;
        var first = new AllomorphEnvironment(
            ConstraintType.Require,
            null,
            Pattern<Word, ShapeNode>.New().Annotation(_table["b"].FeatureStruct).Value
        )
        {
            Name = "before b",
        };
        var second = new AllomorphEnvironment(
            ConstraintType.Require,
            Pattern<Word, ShapeNode>.New().Annotation(_table["d"].FeatureStruct).Value,
            null
        )
        {
            Name = "after d",
        };
        allomorph.Environments.Add(first);
        allomorph.Environments.Add(second);
        Assert.That(Morpher().ParseWord("a", out object trace), Is.Empty);
        Trace failure = Descendants((Trace)trace).Single(t => t.FailureReason == FailureReason.Environments);
        Assert.That(failure.Allomorph, Is.SameAs(allomorph));
        Assert.That(failure.FailureObject, Is.EquivalentTo(new[] { first, second }));
    }

    [Test]
    public void CompoundingAnalysis_ReportsPatternRejection()
    {
        CompoundingRule rule = Compound();
        Word input = AnalysisInput("a");
        input.Freeze();
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Trace failure = Events(input).Single(t => t.Type == TraceType.CompoundingRuleAnalysis);
        Assert.That(failure.FailureReason, Is.EqualTo(FailureReason.Pattern));
        Assert.That(failure.SubruleIndex, Is.Zero);
    }

    [Test]
    public void PhonologicalAnalysis_NoUnapplicationKeepsReasonUnknown()
    {
        var rule = new RewriteRule
        {
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(_table["b"].FeatureStruct).Value,
            Subrules =
            {
                new RewriteSubrule { Rhs = Pattern<Word, ShapeNode>.New().Annotation(_table["d"].FeatureStruct).Value },
            },
        };
        Word input = AnalysisInput("a");
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Trace failure = Events(input).Single(t => t.Type == TraceType.PhonologicalRuleAnalysis);
        Assert.Multiple(() =>
        {
            Assert.That(failure.Source, Is.SameAs(rule));
            Assert.That(failure.SubruleIndex, Is.Zero);
            Assert.That(failure.Input.Shape.ToString(_table, false), Is.EqualTo("a"));
            Assert.That(failure.Output, Is.Null);
            Assert.That(failure.FailureReason, Is.EqualTo(FailureReason.None));
        });
    }

    [TestCase(false, "UnappliedMorphologicalRules")]
    [TestCase(true, "RealizationalFeatureMismatch")]
    public void PartialParse_ReportsCompletionGate(bool realizational, string cause)
    {
        Entry("a");
        IMorphologicalRule rule;
        if (realizational)
        {
            var affix = new RealizationalAffixProcessRule { RealizationalFeatureStruct = Pos("V") };
            affix.Allomorphs.Add(Suffix().Allomorphs[0]);
            rule = affix;
        }
        else
        {
            rule = Suffix();
        }
        _stratum.MorphologicalRules.Add(rule);
        bool synthesis = false;
        Morpher morpher = Morpher();
        morpher.LexEntrySelector = _ =>
        {
            synthesis = true;
            return true;
        };
        morpher.RuleSelector = selected => !synthesis;
        Assert.That(morpher.ParseWord("as", out object trace), Is.Empty);
        Trace failure = Descendants((Trace)trace).Single(t => t.FailureReason == FailureReason.PartialParse);
        Assert.That(failure.PartialParseCause, Is.EqualTo(Enum.Parse<PartialParseCause>(cause)));
    }

    [TestCase(false, "ApplicableTemplatesNotApplied")]
    [TestCase(true, "NonFinalTemplateAppliedLast")]
    public void PartialParse_ReportsTemplateCompletionGate(bool nonFinal, string cause)
    {
        Entry("a");
        var template = new AffixTemplate
        {
            IsFinal = !nonFinal,
            Slots = { new AffixTemplateSlot(Suffix()) { Optional = nonFinal } },
        };
        _stratum.AffixTemplates.Add(template);
        Assert.That(Morpher().ParseWord("a", out object trace), Is.Empty);
        Trace failure = Descendants((Trace)trace).Single(t => t.FailureReason == FailureReason.PartialParse);
        Assert.That(failure.PartialParseCause, Is.EqualTo(Enum.Parse<PartialParseCause>(cause)));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void OptionalSlot_ReportsAppliedAndSkippedBranches(int parallelism)
    {
        var suffix = Suffix();
        var template = new AffixTemplate { Slots = { new AffixTemplateSlot(suffix) { Optional = true } } };
        Word input = AnalysisInput("as");
        input.Freeze();
        Word[] output = template.CompileAnalysisRule(Morpher(parallelism)).Apply(input).ToArray();
        Assert.That(output.Select(w => w.Shape.ToString(_table, false)), Is.EquivalentTo(new[] { "a", "as" }));
        Trace[] slots = Events(input).Where(t => t.Type == TraceType.TemplateSlotAnalysis).ToArray();
        Assert.That(
            slots.Select(t => t.SlotOutcome),
            Is.EquivalentTo(new[] { TemplateSlotOutcome.RuleApplied, TemplateSlotOutcome.OptionalSkipped })
        );
        Assert.That(slots.Single(t => t.SlotOutcome == TemplateSlotOutcome.RuleApplied).SlotRule, Is.SameAs(suffix));
    }

    [Test]
    public void LexicalLookup_InterleavedInputsKeepTheirOwnSynthesisAndCompletion()
    {
        Word first = AnalysisInput("a");
        Word second = new Word(_stratum, _table.Segment("b")) { CurrentTrace = first.CurrentTrace };
        _traceManager.LexicalLookup(_stratum, first);
        _traceManager.LexicalLookup(_stratum, second);
        Word synthesis = first.Clone();
        _traceManager.SynthesizeWord(_language, synthesis);
        _traceManager.LexicalLookupCompleted(_stratum, second, 0, false);
        _traceManager.LexicalLookupCompleted(_stratum, first, 1, false);
        Trace[] lookups = Events(first).Where(t => t.Type == TraceType.LexicalLookup).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(lookups.Select(t => t.LexicalCandidateCount), Is.EqualTo(new[] { 1, 0 }));
            Assert.That(lookups[0].Children.Single().Type, Is.EqualTo(TraceType.WordSynthesis));
            Assert.That(lookups[1].Children, Is.Empty);
        });
    }

    [Test]
    public void LegacyTraceManager_ReceivesExistingCallbacks()
    {
        ITraceManager legacy = System.Reflection.DispatchProxy.Create<ITraceManager, LegacyTraceProxy>();
        var proxy = (LegacyTraceProxy)legacy;
        proxy.Target = _traceManager;
        Word input = AnalysisInput("a");
        var morpher = new Morpher(legacy, _language, 1);
        Assert.That(Suffix().CompileAnalysisRule(morpher).Apply(input), Is.Empty);
        Assert.That(
            proxy.Calls.Count(name => name == nameof(ITraceManager.MorphologicalRuleNotUnapplied)),
            Is.EqualTo(1)
        );
        var template = new AffixTemplate { Slots = { new AffixTemplateSlot(Suffix()) { Optional = true } } };
        Assert.That(template.CompileAnalysisRule(morpher).Apply(input).Count(), Is.EqualTo(1));
        Assert.That(Events(input).Any(t => t.Type == TraceType.TemplateSlotAnalysis), Is.False);
        Assert.That(morpher.ParseWord("b", out object trace), Is.Empty);
        Assert.That(
            Descendants((Trace)trace).Single(t => t.Type == TraceType.LexicalLookup).LexicalCandidateCount,
            Is.Null
        );
    }

    public class LegacyTraceProxy : System.Reflection.DispatchProxy
    {
        public ITraceManager Target { get; set; } = default!;
        public List<string> Calls { get; } = new List<string>();

        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
        {
            Calls.Add(targetMethod!.Name);
            return targetMethod.Invoke(Target, args);
        }
    }

    [TestCase("a", 1)]
    [TestCase("b", 0)]
    public void LexicalGuess_ReportsSeparateCompletedLookup(string form, int count)
    {
        _table.AddNaturalClass(new NaturalClass(_table["a"].FeatureStruct) { Name = "A" });
        _stratum.Entries.Add(
            new LexEntry
            {
                Id = "pattern",
                SyntacticFeatureStruct = Pos("N"),
                Allomorphs = { new RootAllomorph(new Segments(_table, "[A]*", true)) },
            }
        );
        Assert.That(Morpher().ParseWord(form, out object trace, true).Count(), Is.EqualTo(count));
        Trace[] lookups = Descendants((Trace)trace).Where(t => t.Type == TraceType.LexicalLookup).ToArray();
        Assert.That(lookups.Select(t => t.IsLexicalGuess), Is.EqualTo(new[] { false, true }));
        Assert.That(lookups.Select(t => t.LexicalCandidateCount), Is.EqualTo(new[] { 0, count }));
        Assert.That(lookups[1].Children.Count(t => t.Type == TraceType.WordSynthesis), Is.EqualTo(count));
    }

    [Test]
    public void InterruptedLookup_DoesNotReportCompletedCount()
    {
        Entry("a").Allomorphs.Add(new RootAllomorph(new Segments(_table, "b")));
        Morpher morpher = Morpher();
        morpher.MaxAlternatives = 1;
        object trace = null!;
        Assert.Throws<MaxAlternativesExceededException>(() => morpher.ParseWord("a", out trace).ToArray());
        Assert.That(
            Descendants((Trace)trace).Single(t => t.Type == TraceType.LexicalLookup).LexicalCandidateCount,
            Is.Null
        );
    }

    [TestCase("MaxStemCount")]
    [TestCase("MaxApplicationCount")]
    [TestCase("OutputSyntacticFeatureStruct")]
    public void CompoundingAnalysis_ReportsRuleGate(string reason)
    {
        CompoundingRule rule = Compound();
        Word input = AnalysisInput("ab");
        Morpher morpher = Morpher();
        switch (reason)
        {
            case "MaxStemCount":
                morpher.MaxStemCount = 1;
                break;
            case "MaxApplicationCount":
                input.MorphologicalRuleUnapplied(rule);
                break;
            default:
                input.SyntacticFeatureStruct = Pos("N");
                rule.OutSyntacticFeatureStruct = Pos("V");
                break;
        }
        input.Freeze();
        Assert.That(rule.CompileAnalysisRule(morpher).Apply(input), Is.Empty);
        Trace failure = Events(input).Single(t => t.Type == TraceType.CompoundingRuleAnalysis);
        Assert.That(failure.FailureReason, Is.EqualTo(Enum.Parse<FailureReason>(reason)));
        Assert.That(failure.SubruleIndex, Is.EqualTo(-1));
        Assert.That(failure.FailureObject, Is.Not.Null);
    }

    [TestCase("NonHeadLexicalLookup")]
    [TestCase("NonHeadRequiredSyntacticFeatureStruct")]
    [TestCase("NonHeadProdRestrictMprFeatures")]
    public void CompoundingAnalysis_ReportsNonHeadRejection(string reason)
    {
        CompoundingRule rule = Compound();
        LexEntry? nonHead = reason == "NonHeadLexicalLookup" ? null : Entry("b");
        if (reason == "NonHeadRequiredSyntacticFeatureStruct")
            rule.NonHeadRequiredSyntacticFeatureStruct = Pos("V");
        else if (reason == "NonHeadProdRestrictMprFeatures")
            rule.NonHeadProdRestrictionsMprFeatures.Add(new MprFeature { Name = "required" });
        Word input = AnalysisInput("ab");
        input.Freeze();
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Trace failure = Events(input).Single(t => t.Type == TraceType.CompoundingRuleAnalysis);
        Assert.That(failure.FailureReason, Is.EqualTo(Enum.Parse<FailureReason>(reason)));
        Assert.That(failure.SubruleIndex, Is.Zero);
        if (nonHead != null)
            Assert.That(failure.Input.CurrentNonHead.RootAllomorph.Morpheme, Is.SameAs(nonHead));
        if (reason == "NonHeadProdRestrictMprFeatures")
            Assert.That(failure.FailureObject, Is.SameAs(nonHead!.MprFeatures));
    }

    [Test]
    public void CompoundingAnalysis_SuccessRetainsExistingTraceType()
    {
        CompoundingRule rule = Compound();
        Entry("b");
        Word input = AnalysisInput("ab");
        input.Freeze();
        Word output = rule.CompileAnalysisRule(Morpher()).Apply(input).Single();
        Assert.That(output.Shape.ToString(_table, false), Is.EqualTo("a"));
        Trace applied = Events(input).Single();
        Assert.That(applied.Type, Is.EqualTo(TraceType.MorphologicalRuleAnalysis));
        Assert.That(applied.Source, Is.SameAs(rule));
        Assert.That(applied.Output, Is.SameAs(output));
    }

    [TestCase(1, false)]
    [TestCase(1, true)]
    [TestCase(2, false)]
    [TestCase(2, true)]
    public void TraceDetails_PreserveMorphologicalAnalyses(int parallelism, bool tracing)
    {
        Entry("a");
        AffixProcessRule suffix = Suffix();
        _stratum.AffixTemplates.Add(
            new AffixTemplate { Slots = { new AffixTemplateSlot(suffix) { Optional = true } } }
        );
        _traceManager.IsTracing = tracing;
        Morpher morpher = Morpher(parallelism);
        Word result = morpher.ParseWord("as").Single();
        Assert.That(
            result.AllomorphsInMorphOrder.Select(a => a.Morpheme),
            Is.EqualTo(new Morpheme[] { _stratum.Entries.Single(), suffix })
        );
        Assert.That(morpher.ParseWord("a").Single().AllomorphsInMorphOrder.Count(), Is.EqualTo(1));
        Assert.That(morpher.ParseWord("bs"), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TemplateSynthesis_ReportsAppliedRuleWithoutFalseRequiredFailure(bool optional)
    {
        AffixProcessRule suffix = Suffix();
        var template = new AffixTemplate { Slots = { new AffixTemplateSlot(suffix) { Optional = optional } } };
        Word input = new Word(Entry("a").PrimaryAllomorph, new FeatureStruct());
        input.MorphologicalRuleUnapplied(suffix);
        _traceManager.AnalyzeWord(_language, input);
        input.Freeze();
        Word[] output = template.CompileSynthesisRule(Morpher()).Apply(input).ToArray();
        Assert.That(output.Length, Is.EqualTo(optional ? 2 : 1));
        Trace[] slots = Events(input).Where(t => t.Type == TraceType.TemplateSlotSynthesis).ToArray();
        Assert.That(slots.Length, Is.EqualTo(optional ? 2 : 1));
        Assert.That(slots.Any(t => t.SlotOutcome == TemplateSlotOutcome.RequiredUnfilled), Is.False);
        Assert.That(slots.Single(t => t.SlotOutcome == TemplateSlotOutcome.RuleApplied).SlotRule, Is.SameAs(suffix));
    }

    [Test]
    public void PartialParse_StratumReportsPendingRule()
    {
        Entry("a");
        AffixProcessRule suffix = Suffix();
        suffix.RequiredStemName = new StemName(Pos("N")) { Name = "stem" };
        _stratum.MorphologicalRules.Add(suffix);
        Assert.That(Morpher().ParseWord("as", out object trace), Is.Empty);
        Trace failure = Descendants((Trace)trace).Single(t => t.FailureReason == FailureReason.PartialParse);
        Assert.That(failure.PartialParseCause, Is.EqualTo(PartialParseCause.UnappliedMorphologicalRules));
        Assert.That(((PartialParseFailure)failure.FailureObject).Rule, Is.SameAs(suffix));
    }

    [Test]
    public void PartialParse_FeatureConflictTakesPrecedenceOverPendingRule()
    {
        Entry("a");
        var realizational = new RealizationalAffixProcessRule { RealizationalFeatureStruct = Pos("V") };
        realizational.Allomorphs.Add(Suffix().Allomorphs[0]);
        AffixProcessRule suffix = Suffix();
        suffix.Allomorphs[0].Rhs[1] = new InsertSegments(_table, "+d");
        _stratum.MorphologicalRules.Add(realizational);
        _stratum.MorphologicalRules.Add(suffix);
        bool synthesis = false;
        Morpher morpher = Morpher();
        morpher.LexEntrySelector = _ =>
        {
            synthesis = true;
            return true;
        };
        morpher.RuleSelector = _ => !synthesis;
        Assert.That(morpher.ParseWord("asd", out object trace), Is.Empty);
        Trace failure = Descendants((Trace)trace).Single(t => t.FailureReason == FailureReason.PartialParse);
        Assert.Multiple(() =>
        {
            Assert.That(failure.Output.IsAllMorphologicalRulesApplied, Is.False);
            Assert.That(failure.PartialParseCause, Is.EqualTo(PartialParseCause.RealizationalFeatureMismatch));
            Assert.That(((PartialParseFailure)failure.FailureObject).Rule, Is.Null);
        });
    }

    [Test]
    public void LexicalLookup_CountsYieldedAllomorphCandidates()
    {
        Entry("a").Allomorphs.Add(new RootAllomorph(new Segments(_table, "b")));
        Assert.That(Morpher().ParseWord("a", out object trace).Count(), Is.EqualTo(1));
        Trace lookup = Descendants((Trace)trace).Single(t => t.Type == TraceType.LexicalLookup);
        Assert.That(lookup.LexicalCandidateCount, Is.EqualTo(2));
        Assert.That(lookup.Children.Count(t => t.Type == TraceType.WordSynthesis), Is.EqualTo(2));
    }

    private Morpher Morpher(int parallelism = 1) => new Morpher(_traceManager, _language, parallelism);

    private FeatureStruct Pos(string id) => FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol(id).Value;

    private Word AnalysisInput(string form)
    {
        var word = new Word(_stratum, _table.Segment(form));
        _traceManager.AnalyzeWord(_language, word);
        return word;
    }

    private LexEntry Entry(string form)
    {
        var entry = new LexEntry
        {
            Id = form,
            SyntacticFeatureStruct = Pos("N"),
            Allomorphs = { new RootAllomorph(new Segments(_table, form)) },
        };
        _stratum.Entries.Add(entry);
        return entry;
    }

    private AffixProcessRule Suffix()
    {
        var rule = new AffixProcessRule { Id = "suffix", Gloss = "suffix" };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("stem").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("stem"), new InsertSegments(_table, "+s") },
            }
        );
        return rule;
    }

    private CompoundingRule Compound()
    {
        var rule = new CompoundingRule { Name = "compound" };
        rule.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(Any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonhead").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(_table, "+"), new CopyFromInput("nonhead") },
            }
        );
        _stratum.MorphologicalRules.Add(rule);
        return rule;
    }

    private static IEnumerable<Trace> Events(Word word) => Descendants((Trace)word.CurrentTrace);

    private static IEnumerable<Trace> Descendants(Trace trace)
    {
        foreach (Trace child in trace.Children)
        {
            yield return child;
            foreach (Trace descendant in Descendants(child))
                yield return descendant;
        }
    }
}
