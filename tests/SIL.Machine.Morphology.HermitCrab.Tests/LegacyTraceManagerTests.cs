using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// A trace manager that implements only ITraceManager must see exactly the rejection callbacks, with exactly the
/// arguments, that it has always seen. Each expectation is a recorded callback sequence:
/// Name(subruleIndex, input|other[, reason, failureObject type]).
/// </summary>
[TestFixture]
public class LegacyTraceManagerTests
{
    private const string Unapplied0 = "MorphologicalRuleNotUnapplied(0, input)";

    private Language _language = default!;
    private Stratum _stratum = default!;
    private CharacterDefinitionTable _table = default!;
    private TraceManager _inner = default!;
    private RecordingProxy _recorder = default!;
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
        _inner = new TraceManager { IsTracing = true };
        ITraceManager plain = System.Reflection.DispatchProxy.Create<ITraceManager, RecordingProxy>();
        _recorder = (RecordingProxy)plain;
        _recorder.Target = _inner;
        _recorder.Plain = plain;
    }

    [Test]
    public void Recorder_IsNotADetailedTraceManager()
    {
        Assert.That(_recorder.Plain, Is.Not.InstanceOf<IDetailedTraceManager>());
    }

    [Test]
    public void AffixPatternFailure()
    {
        Word input = Input("a");
        Assert.That(Suffix().CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.EqualTo(new[] { Unapplied0 }));
    }

    [Test]
    public void AffixMaxApplicationCountGate()
    {
        AffixProcessRule rule = Suffix();
        Word input = Input("as", w => w.MorphologicalRuleUnapplied(rule));
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.Empty);
    }

    [Test]
    public void AffixOutputFeatureStructGate()
    {
        AffixProcessRule rule = Suffix();
        rule.OutSyntacticFeatureStruct = Pos("V");
        Word input = Input("as", w => w.SyntacticFeatureStruct = Pos("N"));
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.Empty);
    }

    [Test]
    public void AffixRequiredFeatureStructGate()
    {
        AffixProcessRule rule = Suffix();
        // the output gate is built when the rule is compiled, so Required is set afterwards to reach the per-word gate
        IRule<Word, ShapeNode> analysis = rule.CompileAnalysisRule(Morpher());
        rule.RequiredSyntacticFeatureStruct = Pos("V");
        Word input = Input("as", w => w.SyntacticFeatureStruct = Pos("N"));
        Assert.That(analysis.Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.EqualTo(new[] { Unapplied0 }));
    }

    [Test]
    public void RealizationalPatternFailure()
    {
        Word input = Input("a");
        Assert.That(Realizational().CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.EqualTo(new[] { Unapplied0 }));
    }

    [Test]
    public void RealizationalFeatureStructGate()
    {
        Word input = Input("as", w => w.RealizationalFeatureStruct = Pos("N"));
        Assert.That(Realizational().CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.Empty);
    }

    [Test]
    public void CompoundingNonHeadRequiredFeatureStructGate()
    {
        Entry("b");
        CompoundingRule rule = Compound();
        rule.NonHeadRequiredSyntacticFeatureStruct = Pos("V");
        Word input = Input("ab");
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.EqualTo(new[] { Unapplied0 }));
    }

    [Test]
    public void CompoundingNonHeadLookupFailure()
    {
        Entry("b");
        Word input = Input("aa");
        Assert.That(Compound().CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.EqualTo(new[] { Unapplied0 }));
    }

    [Test]
    public void CompoundingNonHeadProductivityRestriction()
    {
        Entry("b");
        CompoundingRule rule = Compound();
        rule.NonHeadProdRestrictionsMprFeatures.Add(new MprFeature { Name = "required" });
        Word input = Input("ab");
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Assert.That(
            _recorder.Calls,
            Is.EqualTo(
                new[]
                {
                    "CompoundingRuleNotUnapplied(-1, other, NonHeadProdRestrictMprFeatures, MprFeatureSet)",
                    Unapplied0,
                }
            )
        );
    }

    [Test]
    public void CompoundingHeadRequiredFeatureStructGate()
    {
        Entry("b");
        CompoundingRule rule = Compound();
        rule.HeadRequiredSyntacticFeatureStruct = Pos("V");
        Word input = Input("ab", w => w.SyntacticFeatureStruct = Pos("N"));
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.Empty);
    }

    [Test]
    public void CompoundingMaxStemCountGate()
    {
        Entry("b");
        CompoundingRule rule = Compound();
        Morpher morpher = Morpher();
        morpher.MaxStemCount = 1;
        Word input = Input("ab");
        Assert.That(rule.CompileAnalysisRule(morpher).Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.Empty);
    }

    [Test]
    public void CompoundingMaxApplicationCountGate()
    {
        Entry("b");
        CompoundingRule rule = Compound();
        Word input = Input("ab", w => w.MorphologicalRuleUnapplied(rule));
        Assert.That(rule.CompileAnalysisRule(Morpher()).Apply(input), Is.Empty);
        Assert.That(_recorder.Calls, Is.Empty);
    }

    [Test]
    public void CompoundingSuccess()
    {
        Entry("b");
        Word input = Input("ab");
        Assert.That(Compound().CompileAnalysisRule(Morpher()).Apply(input).Count(), Is.EqualTo(1));
        Assert.That(_recorder.Calls, Is.Empty);
    }

    [Test]
    public void PartialParse_RealizationalMismatchHasNoFailureObject()
    {
        Entry("a");
        var rule = Realizational();
        rule.RealizationalFeatureStruct = Pos("V");
        _stratum.MorphologicalRules.Add(rule);
        Morpher morpher = AnalysisOnly();
        Assert.That(morpher.ParseWord("as", out _), Is.Empty);
        Assert.That(_recorder.Calls.Where(c => c.StartsWith("Failed")), Is.EqualTo(new[] { PartialParseFailed }));
    }

    [Test]
    public void PartialParse_UnappliedRuleHasNoFailureObject()
    {
        Entry("a");
        _stratum.MorphologicalRules.Add(Suffix());
        Morpher morpher = AnalysisOnly();
        Assert.That(morpher.ParseWord("as", out _), Is.Empty);
        Assert.That(_recorder.Calls.Where(c => c.StartsWith("Failed")), Is.EqualTo(new[] { PartialParseFailed }));
    }

    [Test]
    public void PartialParse_StratumPendingRuleHasNoFailureObject()
    {
        Entry("a");
        AffixProcessRule suffix = Suffix();
        suffix.RequiredStemName = new StemName(Pos("N")) { Name = "stem" };
        _stratum.MorphologicalRules.Add(suffix);
        Assert.That(Morpher().ParseWord("as", out _), Is.Empty);
        Assert.That(_recorder.Calls.Where(c => c.StartsWith("Failed")), Is.EqualTo(new[] { PartialParseFailed }));
    }

    [Test]
    public void FullParse_UnparsableWord()
    {
        Assert.That(Morpher().ParseWord("b", out _), Is.Empty);
        Assert.That(_recorder.Calls, Is.Empty);
    }

    private const string PartialParseFailed = "Failed(other, PartialParse, null)";

    // synthesis starts from every lexical entry, so rules are skipped there to leave the word with a pending rule
    private Morpher AnalysisOnly()
    {
        bool synthesis = false;
        Morpher morpher = Morpher();
        morpher.LexEntrySelector = _ =>
        {
            synthesis = true;
            return true;
        };
        morpher.RuleSelector = _ => !synthesis;
        return morpher;
    }

    private Morpher Morpher() => new Morpher(_recorder.Plain, _language, 1);

    private FeatureStruct Pos(string id) => FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol(id).Value;

    private Word Input(string form, Action<Word>? configure = null)
    {
        var word = new Word(_stratum, _table.Segment(form));
        _inner.AnalyzeWord(_language, word);
        configure?.Invoke(word);
        word.Freeze();
        _recorder.Input = word;
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

    private RealizationalAffixProcessRule Realizational()
    {
        var rule = new RealizationalAffixProcessRule { RealizationalFeatureStruct = Pos("V") };
        rule.Allomorphs.Add(Suffix().Allomorphs[0]);
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

    public class RecordingProxy : System.Reflection.DispatchProxy
    {
        public ITraceManager Target { get; set; } = default!;
        public ITraceManager Plain { get; set; } = default!;
        public Word? Input { get; set; }
        public List<string> Calls { get; } = new List<string>();

        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod!.Name)
            {
                case nameof(ITraceManager.MorphologicalRuleNotUnapplied):
                    Calls.Add($"{targetMethod.Name}({args![1]}, {Label(args[2])})");
                    break;
                case nameof(ITraceManager.CompoundingRuleNotUnapplied):
                    Calls.Add(
                        $"{targetMethod.Name}({args![1]}, {Label(args[2])}, {args[3]}, {args[4]?.GetType().Name ?? "null"})"
                    );
                    break;
                case nameof(ITraceManager.Failed):
                    Calls.Add(
                        $"{targetMethod.Name}({Label(args![1])}, {args[2]}, {args[4]?.GetType().Name ?? "null"})"
                    );
                    break;
            }
            return targetMethod.Invoke(Target, args);
        }

        private string Label(object? word) => ReferenceEquals(word, Input) ? "input" : "other";
    }
}
