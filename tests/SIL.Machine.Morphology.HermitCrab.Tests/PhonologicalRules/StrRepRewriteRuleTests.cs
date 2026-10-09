using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

public class StrRepRewriteRuleTests
{
    [Test]
    public void LiteralRewriteAfterSegmentClassAnalyzesKadAndRejectsKat()
    {
        var table = new CharacterDefinitionTable();
        foreach (string segment in new[] { "a", "t", "d", "k" })
            table.AddSegment(segment);
        var language = new Language();
        language.SyntacticFeatureSystem.AddPartsOfSpeech(new FeatureSymbol("N"));
        var stratum = new Stratum(table);
        language.Strata.Add(stratum);
        var entry = new LexEntry
        {
            Id = "kat",
            SyntacticFeatureStruct = FeatureStruct.New(language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        entry.Allomorphs.Add(new RootAllomorph(new Segments(table, "kat")));
        stratum.Entries.Add(entry);
        var vowels = new SegmentNaturalClass(new[] { table["a"] });
        var rule = new RewriteRule { Lhs = Pattern<Word, ShapeNode>.New().Annotation(table["t"].FeatureStruct).Value };
        rule.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>.New().Annotation(table["d"].FeatureStruct).Value,
                LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowels.FeatureStruct).Value,
            }
        );
        stratum.PhonologicalRules.Add(rule);
        var morpher = new Morpher(new TraceManager(), language);

        Assert.That(
            morpher.GenerateWords(entry, Array.Empty<Morpheme>(), new FeatureStruct()),
            Is.EquivalentTo(new[] { "kad" })
        );
        Assert.That(morpher.ParseWord("kad").Select(w => w.RootAllomorph.Morpheme.Id), Is.EqualTo(new[] { "kat" }));
        foreach (string neighbor in new[] { "kat", "kak", "dat", "tat", "kta", "ka" })
            Assert.That(morpher.ParseWord(neighbor), Is.Empty, neighbor);
    }

    [Test]
    public void LiteralRewriteWithoutFeaturesAnalyzesItsSynthesisAndRejectsVacuousUnapplication()
    {
        var table = new CharacterDefinitionTable();
        foreach (string segment in new[] { "m", "p", "x", "u", "a" })
            table.AddSegment(segment);
        var language = new Language();
        language.SyntacticFeatureSystem.AddPartsOfSpeech(new FeatureSymbol("N"));
        var stratum = new Stratum(table);
        language.Strata.Add(stratum);
        var entries = new Dictionary<string, LexEntry>();
        foreach (string form in new[] { "xmuma", "muma", "xuma" })
        {
            var entry = new LexEntry
            {
                Id = form,
                SyntacticFeatureStruct = FeatureStruct.New(language.SyntacticFeatureSystem).Symbol("N").Value,
            };
            entry.Allomorphs.Add(new RootAllomorph(new Segments(table, form)));
            stratum.Entries.Add(entry);
            entries.Add(form, entry);
        }
        var rule = new RewriteRule { Lhs = Pattern<Word, ShapeNode>.New().Annotation(table["m"].FeatureStruct).Value };
        rule.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>.New().Annotation(table["p"].FeatureStruct).Value,
                LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(table["x"].FeatureStruct).Value,
            }
        );
        stratum.PhonologicalRules.Add(rule);
        var morpher = new Morpher(new TraceManager(), language);

        Assert.That(
            morpher.GenerateWords(entries["xmuma"], Array.Empty<Morpheme>(), new FeatureStruct()),
            Is.EquivalentTo(new[] { "xpuma" })
        );
        Assert.That(morpher.ParseWord("xpuma").Select(w => w.RootAllomorph.Morpheme.Id), Is.EqualTo(new[] { "xmuma" }));
        Assert.That(morpher.ParseWord("muma").Select(w => w.RootAllomorph.Morpheme.Id), Is.EqualTo(new[] { "muma" }));
        Assert.That(morpher.ParseWord("xuma").Select(w => w.RootAllomorph.Morpheme.Id), Is.EqualTo(new[] { "xuma" }));
        Assert.That(morpher.ParseWord("xmuma"), Is.Empty);
        Assert.That(morpher.ParseWord("puma"), Is.Empty);

        var analysisRule = new AnalysisRewriteRule(morpher, rule);
        var word = new Word(stratum, new Segments(table, "xpuma").Shape.Clone());
        Assert.That(analysisRule.Apply(word), Is.Not.Empty);
        word.ResetDirty();
        Assert.That(analysisRule.Apply(word), Is.Empty);
    }
}
