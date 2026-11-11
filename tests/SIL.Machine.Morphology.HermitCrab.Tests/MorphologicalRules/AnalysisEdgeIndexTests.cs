using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

[TestFixture]
public class AnalysisEdgeIndexTests : HermitCrabTestBase
{
    private static FeatureStruct AnySegment => FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    private AffixProcessAllomorph Affix(string prefix, string suffix)
    {
        var allo = new AffixProcessAllomorph
        {
            Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(AnySegment).OneOrMore.Value },
        };
        if (prefix.Length > 0)
            allo.Rhs.Add(new InsertSegments(Table3, prefix));
        allo.Rhs.Add(new CopyFromInput("1"));
        if (suffix.Length > 0)
            allo.Rhs.Add(new InsertSegments(Table3, suffix));
        return allo;
    }

    private static AffixProcessRule Rule(params AffixProcessAllomorph[] allomorphs)
    {
        var rule = new AffixProcessRule
        {
            Id = "R1",
            Name = "r1",
            Gloss = "R1",
        };
        foreach (AffixProcessAllomorph allo in allomorphs)
            rule.Allomorphs.Add(allo);
        return rule;
    }

    private Word FrozenInput(string word, Action<Shape>? edit = null)
    {
        Shape shape = Morphophonemic.CharacterDefinitionTable.Segment(word);
        edit?.Invoke(shape);
        var input = new Word(Morphophonemic, shape);
        input.Freeze();
        return input;
    }

    private sealed class Run
    {
        public string[] Output = [];
        public int IndexRejected;
        public int PrefilterRejected;
    }

    private Run Apply(AffixProcessRule rule, Word input, bool indexEnabled, ITraceManager? traceManager = null)
    {
        var run = new Run();
        var morpher = new Morpher(traceManager ?? TraceManager, Language, maxDegreeOfParallelism: 1)
        {
            EdgeIndexEnabled = indexEnabled,
            EdgeIndexRejected = () => run.IndexRejected++,
            EdgePrefilterRejected = () => run.PrefilterRejected++,
        };
        run.Output = new AnalysisAffixProcessRule(morpher, rule)
            .Apply(input)
            .Select(w => w.ToString())
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        return run;
    }

    private (Run On, Run Off) ApplyBoth(AffixProcessRule rule, Word input)
    {
        Run on = Apply(rule, input, true);
        Run off = Apply(rule, input, false);
        Assert.That(on.Output, Is.EqualTo(off.Output));
        Assert.That(off.IndexRejected, Is.Zero);
        return (on, off);
    }

    private static string[] ParseSignatures(Morpher morpher, string word) =>
        morpher
            .ParseWord(word)
            .Select(MorpherTests.WordAnalysisSignature)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

    [Test]
    public void EveryAllomorphFailingTheEdge_SkipsTheRule()
    {
        AffixProcessRule rule = Rule(Affix("", "d"), Affix("", "t"));

        (Run on, Run off) = ApplyBoth(rule, FrozenInput("sag"));

        Assert.That(on.Output, Is.Empty);
        Assert.That(on.IndexRejected, Is.EqualTo(1));
        Assert.That(on.PrefilterRejected, Is.Zero);
        Assert.That(off.PrefilterRejected, Is.EqualTo(2));
    }

    [Test]
    public void OneAllomorphPassingTheEdge_KeepsTheRule()
    {
        AffixProcessRule rule = Rule(Affix("", "d"), Affix("", "g"));

        (Run on, Run off) = ApplyBoth(rule, FrozenInput("sag"));

        Assert.That(on.Output, Is.Not.Empty);
        Assert.That(on.IndexRejected, Is.Zero);
        Assert.That(on.PrefilterRejected, Is.EqualTo(1));
        Assert.That(off.PrefilterRejected, Is.EqualTo(1));
    }

    [Test]
    public void AllomorphsFailingOnDifferentSides_KeepTheRule()
    {
        // The index skips only on one side at a time, so the per-allomorph prefilter still rejects both.
        AffixProcessRule rule = Rule(Affix("z", "d"), Affix("s", "g"));

        (Run on, _) = ApplyBoth(rule, FrozenInput("sad"));

        Assert.That(on.Output, Is.Empty);
        Assert.That(on.IndexRejected, Is.Zero);
        Assert.That(on.PrefilterRejected, Is.EqualTo(2));
    }

    [Test]
    public void AllomorphWithoutAnEdgeConstraint_KeepsTheRule()
    {
        AffixProcessRule rule = Rule(Affix("", "d"), Affix("z", ""));

        (Run on, _) = ApplyBoth(rule, FrozenInput("sag"));

        Assert.That(on.Output, Is.Empty);
        Assert.That(on.IndexRejected, Is.Zero);
    }

    [Test]
    public void OptionalEdgeNode_KeepsTheRule()
    {
        AffixProcessRule rule = Rule(Affix("", "d"), Affix("", "t"));

        (Run on, _) = ApplyBoth(rule, FrozenInput("sadg", shape => shape.Last.Annotation.Optional = true));

        Assert.That(on.Output, Is.Not.Empty);
        Assert.That(on.IndexRejected, Is.Zero);
    }

    [Test]
    public void EdgeMatchingNoCharacterDefinition_KeepsTheRule()
    {
        AffixProcessRule rule = Rule(Affix("", "d"), Affix("", "t"));
        Word input = FrozenInput(
            "sag",
            shape =>
                shape.Last.Annotation.FeatureStruct.RemoveValue(Language.PhonologicalFeatureSystem.GetFeature("cont"))
        );

        (Run on, Run off) = ApplyBoth(rule, input);

        Assert.That(on.Output, Is.Empty);
        Assert.That(on.IndexRejected, Is.Zero);
        Assert.That(on.PrefilterRejected, Is.EqualTo(2));
        Assert.That(off.PrefilterRejected, Is.EqualTo(2));
    }

    [TestCase("sag", false)]
    [TestCase("zad", true)]
    public void AllInsertAllomorph_SkippedOnlyWhenItsEdgeFails(string word, bool matches)
    {
        var allInsert = new AffixProcessAllomorph { Rhs = { new InsertSegments(Table3, "zad") } };
        AffixProcessRule rule = Rule(allInsert, Affix("z", "d"));

        (Run on, _) = ApplyBoth(rule, FrozenInput(word));

        Assert.That(on.Output.Any(), Is.EqualTo(matches));
        Assert.That(on.IndexRejected, Is.EqualTo(matches ? 0 : 1));
    }

    [Test]
    public void Tracing_UsesThePerAllomorphPathWithTheSameEvents()
    {
        AffixProcessRule rule = Rule(Affix("", "d"), Affix("", "t"));
        var traceManager = new TraceManager { IsTracing = true };
        Word input = FrozenInput("sag");

        string[] Traced(bool indexEnabled, out Run run)
        {
            traceManager.AnalyzeWord(Language, input);
            var root = (Trace)input.CurrentTrace;
            run = Apply(rule, input, indexEnabled, traceManager);
            return root.Children.Select(t => $"{t.Type} {t.Source} {t.SubruleIndex} {t.Input} {t.Output}").ToArray();
        }

        string[] on = Traced(true, out Run onRun);
        string[] off = Traced(false, out _);
        Assert.That(on, Is.EqualTo(off));
        Assert.That(on, Has.Length.EqualTo(2));
        Assert.That(onRun.IndexRejected, Is.Zero);
    }

    [Test]
    public void ParseWord_SyntheticGrammar_IndexMatchesNoIndex()
    {
        Morphophonemic.MorphologicalRules.Add(Rule(Affix("", "d"), Affix("", "t")));
        foreach (int degree in new[] { 1, 0 })
        {
            int rejected = 0;
            var on = new Morpher(TraceManager, Language, degree)
            {
                EdgeIndexRejected = () => Interlocked.Increment(ref rejected),
            };
            var off = new Morpher(TraceManager, Language, degree) { EdgeIndexEnabled = false };
            foreach (string word in new[] { "sag", "sagd", "sagt", "pi", "bubu" })
            {
                Assert.That(
                    ParseSignatures(on, word),
                    Is.EqualTo(ParseSignatures(off, word)),
                    $"'{word}' degree {degree}"
                );
            }
            Assert.That(ParseSignatures(on, "sagd"), Is.Not.Empty);
            Assert.That(rejected, Is.GreaterThan(0), $"degree {degree}");
        }
    }

    [TestCase(1)]
    [TestCase(0)]
    public void ParseWord_StressFixture_IndexMatchesNoIndex(int maxDegreeOfParallelism)
    {
        static string Fixture(string file) =>
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Research",
                "StressFixtures",
                "early-lexicon-agglutinative",
                file
            );
        Language language = XmlLanguageLoader.Load(Fixture("grammar.xml"));
        string[] words = File.ReadAllLines(Fixture("words.txt")).Where(line => line.Length > 0).Take(60).ToArray();
        int rejected = 0;
        var on = new Morpher(TraceManager, language, maxDegreeOfParallelism)
        {
            EdgeIndexRejected = () => Interlocked.Increment(ref rejected),
        };
        var off = new Morpher(TraceManager, language, maxDegreeOfParallelism) { EdgeIndexEnabled = false };

        int parsed = 0;
        foreach (string word in words)
        {
            string[] expected = ParseSignatures(off, word);
            Assert.That(ParseSignatures(on, word), Is.EqualTo(expected), $"'{word}' parse differs with the index");
            if (expected.Length > 0)
                parsed++;
        }
        Assert.That(parsed, Is.GreaterThan(0));
        Assert.That(rejected, Is.GreaterThan(0));
    }
}
