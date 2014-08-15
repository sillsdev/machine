using NUnit.Framework;
using SIL.HermitCrab.MorphologicalRules;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.Tests
{
	public class LexEntryTests : HermitCrabTestBase
	{
		[Test]
		public void DisjunctiveAllomorphs()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var edSuffix = new AffixProcessRule("ed_suffix")
							{
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
							};
			Morphophonemic.MorphologicalRules.Add(edSuffix);
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo1")
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3.Segment("+ɯd"))}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("bazɯd"), "disj ed_suffix");
			Assert.That(morpher.ParseWord("batɯd"), Is.Empty);
			Assert.That(morpher.ParseWord("badɯd"), Is.Empty);
			Assert.That(morpher.ParseWord("basɯd"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("bas"), "disj");
		}

		[Test]
		public void FreeFluctuation()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var voicelessCons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("vd-").Value;
			var alvStop = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident-")
				.Symbol("alveolar")
				.Symbol("nasal-").Value;
			var d = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident-")
				.Symbol("del_rel-")
				.Symbol("alveolar")
				.Symbol("nasal-")
				.Symbol("vd+").Value;

			var edSuffix = new AffixProcessRule("ed_suffix")
							{
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
							};
			Morphophonemic.MorphologicalRules.Add(edSuffix);
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(alvStop).Value
												},
											Rhs = {new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3.Segment("+ɯd"))}
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo2")
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3.Segment("+t"))}
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo3")
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3.Segment("+")), new InsertShapeNode(d)}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("tazd"), "free ed_suffix");
			AssertMorphsEqual(morpher.ParseWord("tast"), "free ed_suffix");
			AssertMorphsEqual(morpher.ParseWord("badɯd"), "disj ed_suffix");
			AssertMorphsEqual(morpher.ParseWord("batɯd"), "disj ed_suffix");
		}

		[Test]
		public void StemNames()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var edSuffix = new AffixProcessRule("ed_suffix")
							{
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("tense").EqualTo("past")).Value
							};
			Morphophonemic.MorphologicalRules.Add(edSuffix);
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo1")
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3.Segment("+ɯd"))}
										});

			var sSuffix = new AffixProcessRule("s_suffix")
							{
								Gloss = "PRES",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("tense").EqualTo("pres")).Value
							};
			Morphophonemic.MorphologicalRules.Add(sSuffix);
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph("s_suffix_allo1")
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3.Segment("+s"))}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sadɯd"), "stemname ed_suffix");
			Assert.That(morpher.ParseWord("sanɯd"), Is.Empty);
			Assert.That(morpher.ParseWord("sads"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("sans"), "stemname s_suffix");
			Assert.That(morpher.ParseWord("sad"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("san"), "stemname");
		}
	}
}
