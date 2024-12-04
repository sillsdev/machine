using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public class MorpherTests : HermitCrabTestBase
{
    [Test]
    public void AnalyzeWord_CanAnalyze_ReturnsCorrectAnalysis()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") }
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(TraceManager, Language);

        Assert.That(
            morpher.AnalyzeWord("sagd"),
            Is.EquivalentTo(new[] { new WordAnalysis(new IMorpheme[] { Entries["32"], edSuffix }, 0, "V") })
        );

        SetRuleOrder(MorphologicalRuleOrder.Linear);
        Assert.That(
            morpher.AnalyzeWord("sagd"),
            Is.EquivalentTo(new[] { new WordAnalysis(new IMorpheme[] { Entries["32"], edSuffix }, 0, "V") })
        );
    }

    [Test]
    public void AnalyzeWord_CannotAnalyze_ReturnsEmptyEnumerable()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") }
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(TraceManager, Language);

        Assert.That(morpher.AnalyzeWord("sagt"), Is.Empty);
    }

    [Test]
    public void AnalyzeWord_CanGuess_ReturnsCorrectAnalysis()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") }
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var naturalClass = new NaturalClass(new FeatureStruct()) { Name = "Any" };
        Morphophonemic.CharacterDefinitionTable.AddNaturalClass(naturalClass);
        AddEntry("pattern", new FeatureStruct(), Morphophonemic, "[Any]*");

        var morpher = new Morpher(TraceManager, Language);

        Assert.That(morpher.AnalyzeWord("gag"), Is.Empty);
        Assert.That(morpher.AnalyzeWord("gagd"), Is.Empty);
        var analyses = morpher.AnalyzeWord("gag", true).ToList();
        Assert.That(analyses[0].ToString(), Is.EquivalentTo("[*gag]"));
        var analyses2 = morpher.AnalyzeWord("gagd", true).ToList();
        Assert.That(analyses2[0].ToString(), Is.EquivalentTo("[*gag ed_suffix]"));
    }

    [Test]
    public void GenerateWords_CanGenerate_ReturnsCorrectWord()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var siPrefix = new AffixProcessRule
        {
            Id = "3SG",
            Name = "si_prefix",
            Gloss = "3SG",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
        };
        siPrefix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, "si+"), new CopyFromInput("1") }
            }
        );
        Morphophonemic.MorphologicalRules.Add(siPrefix);

        var edSuffix = new AffixProcessRule
        {
            Id = "PAST",
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+ɯd") }
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(TraceManager, Language);

        var analysis = new WordAnalysis(new IMorpheme[] { siPrefix, Entries["33"], edSuffix }, 1, "V");

        string[] words = morpher.GenerateWords(analysis).ToArray();
        Assert.That(words, Is.EquivalentTo(new[] { "sisasɯd" }));
    }

    [Test]
    public void GenerateWords_CannotGenerate_ReturnsEmptyEnumerable()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Id = "PL",
            Name = "ed_suffix",
            Gloss = "PL",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+ɯd") }
            }
        );
        Morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(TraceManager, Language);

        var analysis = new WordAnalysis(new IMorpheme[] { Entries["32"], edSuffix }, 0, "V");
        Assert.That(morpher.GenerateWords(analysis), Is.Empty);
    }

    [Test]
    public void TestMatchNodesWithPattern()
    {
        Morpher morpher = new Morpher(TraceManager, Language);
        Feature feat1 = new StringFeature("1");
        Feature feat2 = new StringFeature("2");
        FeatureValue valueA = new StringFeatureValue("A");
        FeatureValue valueB = new StringFeatureValue("B");
        FeatureStruct fs1A = new FeatureStruct();
        FeatureStruct fs2B = new FeatureStruct();
        fs1A.AddValue(feat1, valueA);
        fs2B.AddValue(feat2, valueB);

        // Test feature matching.
        List<ShapeNode> nodesfs1A = new List<ShapeNode> { new ShapeNode(fs1A) };
        List<ShapeNode> nodesfs2B = new List<ShapeNode> { new ShapeNode(fs2B) };
        var fs1A2B = morpher.MatchNodesWithPattern(nodesfs1A, nodesfs2B);
        Assert.That(
            fs1A2B.ToList()[0][0].Annotation.FeatureStruct.GetValue(feat1).ToString(),
            Is.EqualTo(valueA.ToString())
        );
        Assert.That(
            fs1A2B.ToList()[0][0].Annotation.FeatureStruct.GetValue(feat2).ToString(),
            Is.EqualTo(valueB.ToString())
        );

        IList<ShapeNode> noNodes = GetNodes("");
        IList<ShapeNode> oneNode = GetNodes("a");
        IList<ShapeNode> twoNodes = GetNodes("aa");
        IList<ShapeNode> threeNodes = GetNodes("aaa");
        IList<ShapeNode> fourNodes = GetNodes("aaaa");
        var naturalClass = new NaturalClass(new FeatureStruct()) { Name = "Any" };
        Table2.AddNaturalClass(naturalClass);

        // Test sequences.
        Assert.That(morpher.MatchNodesWithPattern(oneNode, GetNodes("i")), Is.Empty);
        Assert.That(
            morpher.MatchNodesWithPattern(oneNode, oneNode),
            Is.EqualTo(new List<IList<ShapeNode>> { oneNode })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(twoNodes, twoNodes),
            Is.EquivalentTo(new List<IList<ShapeNode>> { twoNodes })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(threeNodes, threeNodes),
            Is.EquivalentTo(new List<IList<ShapeNode>> { threeNodes })
        );

        // Test optionality.
        IList<ShapeNode> optionalPattern = GetNodes("([Any])");
        Assert.That(
            morpher.MatchNodesWithPattern(noNodes, optionalPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { noNodes })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(oneNode, optionalPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { oneNode })
        );
        Assert.That(morpher.MatchNodesWithPattern(twoNodes, optionalPattern), Is.Empty);

        // Test ambiguity.
        // (It is up to the caller to eliminate duplicates.)
        IList<ShapeNode> optionalPattern2 = GetNodes("([Any])([Any])");
        Assert.That(
            morpher.MatchNodesWithPattern(noNodes, optionalPattern2),
            Is.EquivalentTo(new List<IList<ShapeNode>> { noNodes })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(oneNode, optionalPattern2),
            Is.EquivalentTo(new List<IList<ShapeNode>> { oneNode, oneNode })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(twoNodes, optionalPattern2),
            Is.EquivalentTo(new List<IList<ShapeNode>> { twoNodes })
        );
        Assert.That(morpher.MatchNodesWithPattern(threeNodes, optionalPattern2), Is.Empty);

        // Test Kleene star.
        IList<ShapeNode> starPattern = GetNodes("[Any]*");
        Assert.That(
            morpher.MatchNodesWithPattern(noNodes, starPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { noNodes })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(oneNode, starPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { oneNode })
        );
        Assert.That(
            morpher.MatchNodesWithPattern(twoNodes, starPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { twoNodes })
        );

        // Test Kleene plus look alike ("+" is a boundary marker).
        IList<ShapeNode> plusPattern = GetNodes("[Any]+");
        Assert.That(morpher.MatchNodesWithPattern(noNodes, plusPattern), Is.Empty);
        Assert.That(
            morpher.MatchNodesWithPattern(oneNode, plusPattern),
            Is.EquivalentTo(new List<IList<ShapeNode>> { oneNode })
        );
        Assert.That(morpher.MatchNodesWithPattern(twoNodes, plusPattern), Is.Empty);
    }

    IList<ShapeNode> GetNodes(string pattern)
    {
        // Use Table2 because it has boundaries defined.
        Shape shape = new Segments(Table2, pattern, true).Shape;
        return shape.GetNodes(shape.Range).ToList();
    }
}
