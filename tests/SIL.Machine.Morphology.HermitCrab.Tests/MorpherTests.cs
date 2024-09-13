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

        // Make a lexical pattern equivalent to Any+.
        ShapeNode node = new ShapeNode(new FeatureStruct());
        node.Annotation.Optional = true;
        node.Annotation.Iterative = true;
        var shape = new Shape(begin => new ShapeNode(
            begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor
        ));
        shape.AddRange(new List<ShapeNode> { node });
        var lexicalPattern = new RootAllomorph(new Segments(Table1, "", shape));

        var morpher = new Morpher(TraceManager, Language);
        morpher.LexicalPatterns.Add(lexicalPattern);

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
        FeatureStruct fs1B = new FeatureStruct();
        FeatureStruct fs2B = new FeatureStruct();
        fs1A.AddValue(feat1, valueA);
        fs1B.AddValue(feat1, valueB);
        fs2B.AddValue(feat2, valueB);

        // Test feature matching.
        List<ShapeNode> nodesfs1A = new List<ShapeNode> { new ShapeNode(fs1A) };
        List<ShapeNode> nodesfs1B = new List<ShapeNode> { new ShapeNode(fs1B) };
        List<ShapeNode> nodesfs2B = new List<ShapeNode> { new ShapeNode(fs2B) };
        Assert.That(morpher.MatchNodesWithPattern(nodesfs1A, nodesfs1B), Is.Empty);
        Assert.That(morpher.MatchNodesWithPattern(nodesfs1A, nodesfs1A), Is.EqualTo(new List<List<ShapeNode>> { nodesfs1A }));
        var fs1A2B = morpher.MatchNodesWithPattern(nodesfs1A, nodesfs2B);
        Assert.That(fs1A2B.ToList()[0][0].Annotation.FeatureStruct.GetValue(feat1).ToString(), Is.EqualTo(valueA.ToString()));
        Assert.That(fs1A2B.ToList()[0][0].Annotation.FeatureStruct.GetValue(feat2).ToString(), Is.EqualTo(valueB.ToString()));

        List<ShapeNode> noNodes = new List<ShapeNode> { };
        List<ShapeNode> oneNode = new List<ShapeNode> { new ShapeNode(fs1A) };
        List<ShapeNode> twoNodes = new List<ShapeNode> { new ShapeNode(fs1A), new ShapeNode(fs1A) };
        List<ShapeNode> threeNodes = new List<ShapeNode> { new ShapeNode(fs1A), new ShapeNode(fs1A), new ShapeNode(fs1A) };
        List<ShapeNode> fourNodes = new List<ShapeNode> { new ShapeNode(fs1A), new ShapeNode(fs1A), new ShapeNode(fs1A), new ShapeNode(fs1A) };

        // Test sequences.
        Assert.That(morpher.MatchNodesWithPattern(twoNodes, twoNodes), Is.EquivalentTo(new List<List<ShapeNode>> { twoNodes }));
        Assert.That(morpher.MatchNodesWithPattern(threeNodes, threeNodes), Is.EquivalentTo(new List<List<ShapeNode>> { threeNodes }));

        // Test optionality.
        ShapeNode optionalNode = new ShapeNode(fs1A);
        optionalNode.Annotation.Optional = true;
        List<ShapeNode> optionalPattern = new List<ShapeNode> { optionalNode };
        Assert.That(morpher.MatchNodesWithPattern(noNodes, optionalPattern), Is.EquivalentTo(new List<List<ShapeNode>> { noNodes }));
        Assert.That(morpher.MatchNodesWithPattern(oneNode, optionalPattern), Is.EquivalentTo(new List<List<ShapeNode>> { oneNode }));
        Assert.That(morpher.MatchNodesWithPattern(twoNodes, optionalPattern), Is.Empty);

        // Test Kleene star.
        ShapeNode starNode = new ShapeNode(fs1A);
        starNode.Annotation.Optional = true;
        starNode.Annotation.Iterative = true;
        List<ShapeNode> starPattern = new List<ShapeNode> { starNode };
        Assert.That(morpher.MatchNodesWithPattern(noNodes, starPattern), Is.EquivalentTo(new List<List<ShapeNode>> { noNodes }));
        var result = morpher.MatchNodesWithPattern(oneNode, starPattern);
        Assert.That(morpher.MatchNodesWithPattern(oneNode, starPattern), Is.EquivalentTo(new List<List<ShapeNode>> { oneNode }));
        Assert.That(morpher.MatchNodesWithPattern(twoNodes, starPattern), Is.EquivalentTo(new List<List<ShapeNode>> { twoNodes }));

        // Test Kleene plus.
        ShapeNode plusNode = new ShapeNode(fs1A);
        plusNode.Annotation.Iterative = true;
        List<ShapeNode> plusPattern = new List<ShapeNode> { plusNode };
        Assert.That(morpher.MatchNodesWithPattern(noNodes, plusPattern), Is.Empty);
        Assert.That(morpher.MatchNodesWithPattern(oneNode, plusPattern), Is.EquivalentTo(new List<List<ShapeNode>> { oneNode }));
        Assert.That(morpher.MatchNodesWithPattern(twoNodes, plusPattern), Is.EquivalentTo(new List<List<ShapeNode>> { twoNodes }));
    }
}
