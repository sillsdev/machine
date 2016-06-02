using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab.Tests
{
	[TestFixture]
	public class HermitCrabMorphologicalAnalyzerTests : HermitCrabTestBase
	{
		[Test]
		public void AnalyzeWord_CanAnalyze_ReturnsCorrectAnalysis()
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
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+d")}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);

			var sourceAnalyzer = new HermitCrabMorphologicalAnalyzer(GetMorphemeId, GetCategory, morpher);

			Assert.That(sourceAnalyzer.AnalyzeWord("sagd"), Is.EquivalentTo(new[]
			{
				new WordAnalysis(new[]
				{
					new Morphology.Morpheme("32", "V", "32", MorphemeType.Stem),
					new Morphology.Morpheme("PAST", "V", "PAST", MorphemeType.Affix)
				}, 0, "V")
			}));
		}

		[Test]
		public void AnalyzeWord_CannotAnalyze_ReturnsEmptyEnumerable()
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
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+d")}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);

			var sourceAnalyzer = new HermitCrabMorphologicalAnalyzer(GetMorphemeId, GetCategory, morpher);

			Assert.That(sourceAnalyzer.AnalyzeWord("sagt"), Is.Empty);
		}
	}
}
