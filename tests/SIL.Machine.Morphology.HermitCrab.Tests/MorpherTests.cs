using System.Linq;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
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
    }
}
