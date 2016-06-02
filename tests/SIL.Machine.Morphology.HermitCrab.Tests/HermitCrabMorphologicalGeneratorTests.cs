using System.Linq;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab.Tests
{
	[TestFixture]
	public class HermitCrabMorphologicalGeneratorTests : HermitCrabTestBase
	{
		[Test]
		public void GenerateWords_CanGenerate_ReturnsCorrectWord()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var siPrefix = new AffixProcessRule
							{
								Name = "si_prefix",
								Gloss = "3SG",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
							};
			Morphophonemic.MorphologicalRules.Add(siPrefix);
			siPrefix.Allomorphs.Add(new AffixProcessAllomorph
			                       	{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new InsertShape(Table3, "si+"), new CopyFromInput("1")}
			                       	});

			var edSuffix = new AffixProcessRule
							{
								Name = "ed_suffix",
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
							};
			Morphophonemic.MorphologicalRules.Add(edSuffix);
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+ɯd")}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);

			var targetGenerator = new HermitCrabMorphologicalGenerator(GetMorphemeId, GetCategory, morpher);

			var analysis = new WordAnalysis(new[]
			{
				new Morphology.Morpheme("3SG", "V", "3SG", MorphemeType.Affix), 
				new Morphology.Morpheme("33", "V", "33", MorphemeType.Stem),
				new Morphology.Morpheme("PAST", "V", "PAST", MorphemeType.Affix)
			}, 1, "V");

			string[] words = targetGenerator.GenerateWords(analysis).ToArray();
			Assert.That(words, Is.EquivalentTo(new[] {"sisasɯd"}));
		}

		[Test]
		public void GenerateWords_CannotGenerate_ReturnsEmptyEnumerable()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var edSuffix = new AffixProcessRule
							{
								Name = "ed_suffix",
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
							};
			Morphophonemic.MorphologicalRules.Add(edSuffix);
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+ɯd")}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);

			var targetGenerator = new HermitCrabMorphologicalGenerator(GetMorphemeId, GetCategory, morpher);

			var analysis = new WordAnalysis(new[]
			{
				new Morphology.Morpheme("32", "V", "32", MorphemeType.Stem),
				new Morphology.Morpheme("3SG", "V", "3SG", MorphemeType.Affix)
			}, 0, "V");

			Assert.That(targetGenerator.GenerateWords(analysis), Is.Empty);
		}
	}
}
