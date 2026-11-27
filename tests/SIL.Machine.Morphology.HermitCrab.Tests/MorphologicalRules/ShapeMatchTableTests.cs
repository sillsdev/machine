using System.Text;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

[TestFixture]
public class ShapeMatchTableTests : HermitCrabTestBase
{
    private static string GrammarPath(int k) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "ShapeMatchTable", $"g{k}.xml");

    private sealed class Run
    {
        public string[] Results = Array.Empty<string>();
        public string Trace = "";
        public Dictionary<string, int> Matches = new();
    }

    private static Run ParseWith(
        Func<ITraceManager, Morpher> create,
        bool tableEnabled,
        string word,
        bool tracing,
        bool guessRoot = false
    )
    {
        var run = new Run();
        var traceManager = new TraceManager { IsTracing = tracing };
        Morpher morpher = create(traceManager);
        morpher.ShapeMatchTableEnabled = tableEnabled;
        var matches = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        morpher.ShapeMatched = (rule, input) => matches.AddOrUpdate($"{rule.Name} on {input}", 1, (_, n) => n + 1);
        run.Results = morpher.ParseWord(word, out object trace, guessRoot).Select(Describe).ToArray();
        run.Matches = matches.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (tracing)
            run.Trace = Flatten((Trace)trace, 0);
        return run;
    }

    private static string Describe(Word word) =>
        word.ToString() + "|" + MorpherTests.WordAnalysisSignature(word) + "|" + word.SyntacticFeatureStruct;

    private static string Flatten(Trace trace, int depth)
    {
        var sb = new StringBuilder();
        sb.Append(' ', depth)
            .Append(trace.Type)
            .Append(' ')
            .Append(trace.Source?.Name)
            .Append('/')
            .Append(trace.SubruleIndex)
            .Append(' ')
            .Append(trace.InputText)
            .Append(" -> ")
            .Append(trace.OutputText)
            .Append(' ')
            .Append(trace.FailureReason)
            .AppendLine();
        foreach (Trace child in trace.Children)
            sb.Append(Flatten(child, depth + 1));
        return sb.ToString();
    }

    private static void AssertSameRun(Run on, Run off)
    {
        Assert.That(on.Results, Is.EqualTo(off.Results), "results and their order");
        Assert.That(on.Trace, Is.EqualTo(off.Trace), "trace events");
    }

    [Test]
    public void ReturnedWords_DoNotRetainTheTableRows()
    {
        var morpher = new Morpher(
            new TraceManager(),
            XmlLanguageLoader.Load(GrammarPath(4)),
            maxDegreeOfParallelism: 1
        );
        Word[] results = morpher.ParseWord("katitm").ToArray();

        Assert.That(results, Is.Not.Empty);
        Assert.That(results.Select(w => w.MatchTable), Is.All.Not.Null, "the words still share the table");
        Assert.That(results.Select(w => w.MatchTable.RowCount), Is.All.EqualTo(0), "but it holds no rows");
    }

    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    public void HomophonousSuffixes_EachRuleShapePairMatchesOnce(int k)
    {
        Morpher Create(ITraceManager tm) =>
            new Morpher(tm, XmlLanguageLoader.Load(GrammarPath(k)), maxDegreeOfParallelism: 1);

        foreach (string word in new[] { "katitm", "tikitm" })
        {
            Run off = ParseWith(Create, false, word, tracing: false);
            Run on = ParseWith(Create, true, word, tracing: false);

            TestContext.Out.WriteLine(
                $"k={k} {word}: {on.Matches.Count} (rule, shape) pairs, matches with table {on.Matches.Values.Sum()}, without {off.Matches.Values.Sum()}"
            );
            Assert.That(on.Matches.Values, Is.All.EqualTo(1), "every (rule, shape) pair matches once");
            Assert.That(off.Matches.Values.Max(), Is.EqualTo(k), "without the table a pair matches once per path");
            Assert.That(on.Matches.Keys, Is.EquivalentTo(off.Matches.Keys));
            Assert.That(on.Results, Is.EqualTo(off.Results));
        }
    }

    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    public void HomophonousSuffixes_ResultsOrderAndTraceAreUnchanged(int k)
    {
        Morpher Create(ITraceManager tm) =>
            new Morpher(tm, XmlLanguageLoader.Load(GrammarPath(k)), maxDegreeOfParallelism: 1);

        foreach (string word in new[] { "katitm", "tikitm", "kat", "katm" })
        {
            Run off = ParseWith(Create, false, word, tracing: true);
            Run on = ParseWith(Create, true, word, tracing: true);
            AssertSameRun(on, off);
        }
        Assert.That(ParseWith(Create, true, "katitm", tracing: false).Results, Is.Not.Empty);
    }

    [Test]
    public void HomophonousSuffixes_ParallelParseGivesTheSameResults()
    {
        static Morpher Create(ITraceManager tm) => new Morpher(tm, XmlLanguageLoader.Load(GrammarPath(8)), 4);

        Run off = ParseWith(Create, false, "katitm", tracing: false);
        Run on = ParseWith(Create, true, "katitm", tracing: false);
        Assert.That(
            on.Results.OrderBy(r => r, StringComparer.Ordinal),
            Is.EqualTo(off.Results.OrderBy(r => r, StringComparer.Ordinal))
        );
    }

    // Slot rules run through the same Apply, so their per-path gates stay in front of the table.
    [Test]
    public void TemplateSlotsWithRequiredAndExcludedFeatures_AreUnchanged()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var verb = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;

        LexEntry latinate = AddEntry("lat", verb, Morphophonemic, "zag");
        latinate.MprFeatures.Add(Latinate);
        LexEntry germanic = AddEntry("ger", verb, Morphophonemic, "zug");
        germanic.MprFeatures.Add(Germanic);

        AffixProcessRule Suffix(string id, Action<AffixProcessAllomorph> configure)
        {
            var rule = new AffixProcessRule
            {
                Id = id,
                Name = id,
                Gloss = id,
                RequiredSyntacticFeatureStruct = verb,
            };
            var allomorph = new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "d") },
            };
            configure(allomorph);
            rule.Allomorphs.Add(allomorph);
            return rule;
        }

        AffixProcessRule requiresLatinate = Suffix("REQ", a => a.RequiredMprFeatures.Add(Latinate));
        AffixProcessRule excludesLatinate = Suffix("EXC", a => a.ExcludedMprFeatures.Add(Latinate));
        AffixProcessRule plain = Suffix("PLAIN", _ => { });

        var template = new AffixTemplate { Name = "verb_template", RequiredSyntacticFeatureStruct = verb };
        template.Slots.Add(new AffixTemplateSlot(requiresLatinate, excludesLatinate) { Optional = true });
        template.Slots.Add(new AffixTemplateSlot(plain, excludesLatinate) { Optional = true });
        Morphophonemic.AffixTemplates.Add(template);

        Morpher Create(ITraceManager tm) => new Morpher(tm, Language, maxDegreeOfParallelism: 1);
        foreach (string word in new[] { "zagd", "zugd", "zagdd", "zugdd", "zag" })
        {
            Run off = ParseWith(Create, false, word, tracing: true);
            Run on = ParseWith(Create, true, word, tracing: true);
            AssertSameRun(on, off);
            Assert.That(on.Matches.Values, Is.All.EqualTo(1), word);
            Assert.That(on.Matches.Keys, Is.EquivalentTo(off.Matches.Keys), word);
        }
        Assert.That(ParseWith(Create, true, "zagd", tracing: false).Results, Is.Not.Empty);
        Assert.That(ParseWith(Create, true, "zugdd", tracing: false).Results, Is.Not.Empty);
    }

    [Test]
    public void CompoundingRules_AreNotReadFromTheTable()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new CompoundingRule { Name = "rule1" };
        Allophonic.MorphologicalRules.Add(rule);
        rule.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") },
            }
        );

        Morpher Create(ITraceManager tm) => new Morpher(tm, Language, maxDegreeOfParallelism: 1);
        Run off = ParseWith(Create, false, "pʰutdat", tracing: true);
        Run on = ParseWith(Create, true, "pʰutdat", tracing: true);
        AssertSameRun(on, off);
        Assert.That(on.Results, Is.Not.Empty);
        Assert.That(on.Matches.Keys.Where(key => key.StartsWith("rule1 ", StringComparison.Ordinal)), Is.Empty);
        Assert.That(on.Matches.Keys, Is.EquivalentTo(off.Matches.Keys));
    }

    [Test]
    public void StoredShapesAreFrozenAndNotMutatedByTheWordsBuiltFromThem()
    {
        static Morpher Create(ITraceManager tm) =>
            new Morpher(tm, XmlLanguageLoader.Load(GrammarPath(4)), maxDegreeOfParallelism: 1);
        Morpher morpher = Create(new TraceManager());
        Shape shape = morpher.Language.SurfaceStratum.CharacterDefinitionTable.Segment("katitm");
        var input = new Word(morpher.Language.SurfaceStratum, shape) { MatchTable = new ShapeMatchTable() };
        input.Freeze();

        IMorphologicalRule rule = morpher
            .Language.Strata[0]
            .AffixTemplates.Single()
            .Slots.Single()
            .Rules.First(r => r.Name == "TPL0");
        var analysis = new AnalysisAffixProcessRule(morpher, (AffixProcessRule)rule);
        Word first = analysis.Apply(input).Single();
        Word second = analysis.Apply(input).Single();

        Assert.That(first.Shape.IsFrozen, Is.True);
        Assert.That(second.Shape.IsFrozen, Is.True);
        Assert.That(second.Shape.ValueEquals(first.Shape), Is.True);
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second.Clone().Shape, Is.Not.SameAs(second.Shape), "a clone still owns a mutable copy");
    }
}
