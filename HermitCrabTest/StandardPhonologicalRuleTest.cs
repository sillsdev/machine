using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.HermitCrab;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace HermitCrabTest
{
	public class StandardPhonologicalRuleTest : HermitCrabTestBase
	{
		[Test]
		public void SimpleRules()
		{
			var asp = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("asp+").Value;
			var nonCons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table1.GetSymbolDefinition("t").FeatureStruct).Value;
			var rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			var rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, nonCons).Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("p").FeatureStruct).Value;
			var rule2 = new StandardPhonologicalRule("rule2", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, nonCons).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "pʰitʰ") {Stratum = Allophonic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[p(pʰ)]i[t(tʰ)]", output.Single().ToString());

			input = new Word(Allophonic, "pʰit");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("(pʰ)i(tʰ)", input.ToString());

			input = new Word(Allophonic, "pit");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsTrue(rule2.SynthesisRule.Apply(output.Single(), out output));
			Word result = output.Single();
			result.Stratum = Surface;
			Assert.AreEqual("(pʰ)i(tʰ)", result.ToString());

			input = new Word(Surface, "datʰ") {Stratum = Allophonic};
			Assert.IsFalse(rule2.AnalysisRule.Apply(input, out output));
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("da[t(tʰ)]", output.Single().ToString());

			input = new Word(Allophonic, "dat");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("da(tʰ)", input.ToString());

			input = new Word(Surface, "gab") {Stratum = Allophonic};
			Assert.IsFalse(rule2.AnalysisRule.Apply(input, out output));
			Assert.IsFalse(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("gab", input.ToString());

			input = new Word(Allophonic, "gab");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("gab", input.ToString());
		}

		[Test]
		public void LongDistanceRules()
		{
			var highVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back+").Symbol("round+").Value;
			var rndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("round+").Value;
			var cons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Value;
			var lowVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("low+").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule = new StandardPhonologicalRule("rule", SpanFactory, lhs);
			var rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, rndVowel)
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.SegmentType, lowVowel)
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());
			
			IEnumerable<Word> output;
			var input = new Word(Surface, "bubabu") {Stratum = Allophonic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bubab[iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, "bubabu");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			input = new Word(Allophonic, "bubabi");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			rule = new StandardPhonologicalRule("rule", SpanFactory, lhs);
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.SegmentType, lowVowel)
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.SegmentType, rndVowel).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "bubabu") {Stratum = Allophonic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b[iuyɯ]babu", output.Single().ToString());

			input = new Word(Allophonic, "bubabu");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			input = new Word(Allophonic, "bɯbabu");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());
		}

		[Test]
		public void AnchorRules()
		{
			var cons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Value;
			var vlUnasp = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("vd-").Symbol("asp-").Value;
			var vowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rule = new StandardPhonologicalRule("rule", SpanFactory, lhs);
			var rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vlUnasp).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New().Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "gap") {Stratum = Morphophonemic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("g[a(a̘)][pb]", output.Single().ToString());

			input = new Word(Morphophonemic, "ga̘p");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gap", input.ToString());

			input = new Word(Morphophonemic, "gab");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gap", input.ToString());

			input = new Word(Morphophonemic, "ga+b");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ga+?p", input.ToString());

			rule = new StandardPhonologicalRule("rule", SpanFactory, lhs);
			rightEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, vowel)
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "kab") {Stratum = Morphophonemic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[kg][a(a̘)]b", output.Single().ToString());

			input = new Word(Morphophonemic, "gab");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("kab", input.ToString());

			input = new Word(Morphophonemic, "ga+b");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ka+?b", input.ToString());

			rule = new StandardPhonologicalRule("rule", SpanFactory, lhs);
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "kab") {Stratum = Morphophonemic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[kg][a(a̘)]b", output.Single().ToString());

			input = new Word(Morphophonemic, "gab");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("kab", input.ToString());

			input = new Word(Morphophonemic, "ga+b");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ka+?b", input.ToString());

			rule = new StandardPhonologicalRule("rule", SpanFactory, lhs);
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.AnchorType, LeftSideFS)
				.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "gap") {Stratum = Morphophonemic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("g[a(a̘)][pb]", output.Single().ToString());

			input = new Word(Morphophonemic, "ga̘p");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gap", input.ToString());

			input = new Word(Morphophonemic, "gab");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gap", input.ToString());

			input = new Word(Morphophonemic, "ga+b");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ga+?p", input.ToString());
		}

		[Test]
		public void QuantifierRules()
		{
			var highVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back+").Symbol("round+").Value;
			var cons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Value;
			var lowVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("low+").Value;
			var rndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			var rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New().Value;
			var rightEnv = Pattern<Word, ShapeNode>.New()
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, lowVowel)).LazyRange(1, 2)
				.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, rndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var rule2 = new StandardPhonologicalRule("rule2", SpanFactory, lhs);
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, rndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, lowVowel)).LazyRange(1, 2)
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "bubu") {Stratum = Morphophonemic};
			Assert.IsFalse(rule2.AnalysisRule.Apply(input, out output));
			Assert.IsFalse(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Morphophonemic, "b+ubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("b+?ubu", input.ToString());

			input = new Word(Surface, "bubabu") {Stratum = Allophonic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b[iuyɯ]bab[iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, "bubabu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			input = new Word(Allophonic, "bubabi");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			input = new Word(Allophonic, "bɯbabu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			input = new Word(Allophonic, "bibabi");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bibabi", input.ToString());

			input = new Word(Surface, "bubababu") {Stratum = Allophonic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b[iuyɯ]babab[iuyɯ]", input.ToString());

			input = new Word(Allophonic, "bubababi");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubababu", input.ToString());

			input = new Word(Allophonic, "bibababu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubababu", input.ToString());

			input = new Word(Surface, "bubabababu") {Stratum = Allophonic};
			Assert.IsFalse(rule2.AnalysisRule.Apply(input, out output));
			Assert.IsFalse(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bubabababu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRndVowel).Annotation(HCFeatureSystem.SegmentType, highVowel).LazyRange(0, 2).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "buuubuuu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[iuyɯ][iuyɯ]bu[iuyɯ][iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buuubuuu", input.ToString());
		}

		[Test]
		public void MultipleSegmentRules()
		{
			var highVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back+").Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;
			var t = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("alveolar").Symbol("del_rel-").Symbol("asp-").Symbol("vd-").Symbol("strident-").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			var rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRndVowel).Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "buuubuuu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[iuyɯ][iuyɯ]bu[iuyɯ][iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buuubuuu", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, t).Value;
			var rule2 = new StandardPhonologicalRule("rule2", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRndVowel).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "buuubuuu") {Stratum = Allophonic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("but?[iuyɯ]t?[iuyɯ]but?[iuyɯ]t?[iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buuubuuu", input.ToString());

			input = new Word(Allophonic, "buitibuiti");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buitibuiti", input.ToString());
		}

		[Test]
		public void BoundaryRules()
		{
			var highVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;
			var backRnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back+").Symbol("round+").Value;
			var unbackUnrnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back-").Symbol("round-").Value;
			var unbackUnrndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round-").Value;
			var backVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("back+").Value;
			var unrndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("round-").Value;
			var lowBack = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back+").Symbol("low+").Symbol("high-").Value;
			var bilabialCons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Symbol("bilabial").Value;
			var unvdUnasp = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("vd-").Symbol("asp-").Value;
			var vowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Value;
			var cons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Value;
			var asp = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("asp+").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			var rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, backRndVowel)
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetSymbolDefinition("+").FeatureStruct).Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "buub") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[iuyɯ]b", output.Single().ToString());

			input = new Word(Morphophonemic, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bu+?ub", input.ToString());

			input = new Word(Morphophonemic, "buib");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buib", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unbackUnrnd).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetSymbolDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, unbackUnrndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "biib") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b[iuyɯ]ib", output.Single().ToString());

			input = new Word(Morphophonemic, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bi+?ib", input.ToString());

			input = new Word(Morphophonemic, "buib");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buib", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRndVowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "buub") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[iuyɯ]b", output.Single().ToString());

			input = new Word(Morphophonemic, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bu+?ub", input.ToString());

			input = new Word(Morphophonemic, "buib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buub", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unbackUnrnd).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unbackUnrndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "biib") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b[iuyɯ]ib", output.Single().ToString());

			input = new Word(Morphophonemic, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bi+?ib", input.ToString());

			input = new Word(Morphophonemic, "buib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("biib", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("i").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("b").FeatureStruct).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backVowel).Value;
			var rule2 = new StandardPhonologicalRule("rule2", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("a").FeatureStruct).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetSymbolDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("b").FeatureStruct).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "bab") {Stratum = Morphophonemic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("i?b[a(a̘)uɯo]i?b", output.Single().ToString());

			input = new Word(Morphophonemic, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ba+?b", input.ToString());

			input = new Word(Morphophonemic, "buib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bub", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("u").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("b").FeatureStruct).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unrndVowel).Value;
			rule2 = new StandardPhonologicalRule("rule2", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, lowBack).Value;
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("b").FeatureStruct)
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetSymbolDefinition("+").FeatureStruct).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "bab") {Stratum = Morphophonemic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu?[a(a̘)iɯ]bu?", output.Single().ToString());

			input = new Word(Morphophonemic, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("b+?ab", input.ToString());

			input = new Word(Morphophonemic, "buib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bib", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons)
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetSymbolDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp)
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetSymbolDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "appa") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[a(a̘)][pb][pb][a(a̘)]", output.Single().ToString());

			input = new Word(Morphophonemic, "ab+ba");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ap+?pa", input.ToString());

			input = new Word(Morphophonemic, "abba");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("abba", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons)
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp)
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "appa") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[a(a̘)][pb][pb][a(a̘)]", output.Single().ToString());

			input = new Word(Morphophonemic, "ab+ba");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("ab+?ba", input.ToString());

			input = new Word(Morphophonemic, "abba");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("appa", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.BoundaryType, Table3.GetSymbolDefinition("+").FeatureStruct).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "pʰipʰ") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", output.Single().ToString());

			input = new Word(Allophonic, "pip");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("pip", input.ToString());
		}

		[Test]
		public void CommonFeatureRules()
		{
			var vowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Value;
			var vdLabFric = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("labiodental").Symbol("vd+").Symbol("strident+").Symbol("cont+").Value;

			var lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, Table1.GetSymbolDefinition("p").FeatureStruct).Value;
			var rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			var rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vdLabFric).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "buvu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[pbmfv]u", output.Single().ToString());

			input = new Word(Allophonic, "bupu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buvu", input.ToString());

			input = new Word(Morphophonemic, "b+ubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("b+?ubu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table1.GetSymbolDefinition("v").FeatureStruct).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "buvu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[pbmfv]u", output.Single().ToString());

			input = new Word(Allophonic, "bupu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buvu", input.ToString());

			input = new Word(Morphophonemic, "b+ubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("b+?ubu", input.ToString());
		}

		[Test]
		public void AlphaVariableRules()
		{
			var highVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var cons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Value;
			var nasalCons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Symbol("nasal+").Value;
			var voicelessStop = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("vd-").Symbol("cont-").Value;
			var asp = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("asp+").Value;
			var vowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Value;
			var unasp = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("asp-").Value;
			var k = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Symbol("velar").Symbol("vd-").Symbol("cont-").Symbol("nasal-").Value;
			var g = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Symbol("velar").Symbol("vd+").Symbol("cont-").Symbol("nasal-").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			var rhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, highVowel)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value)
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "bububu") { Stratum = Morphophonemic };
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bub[iuyɯ]b[iuyɯ]", output.Single().ToString());

			input = new Word(Morphophonemic, "bubibi");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bububu", input.ToString());

			input = new Word(Morphophonemic, "bubibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bububu", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, nasalCons).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem)
					.Feature("poa").EqualToVariable("a").Value).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, cons)
					.Feature("poa").EqualToVariable("a").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "mbindiŋg") { Stratum = Morphophonemic };
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[mnŋ]bi[mnŋ]di[mnŋ]g", output.Single().ToString());

			input = new Word(Morphophonemic, "nbinding");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("mbindiŋg", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, voicelessStop)
					.Feature("poa").EqualToVariable("a").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, voicelessStop)
					.Feature("poa").EqualToVariable("a").Value)
				.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "pipʰ") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", output.Single().ToString());

			input = new Word(Allophonic, "pip");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("pi(pʰ)", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table1.GetSymbolDefinition("f").FeatureStruct).Value;
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, vowel)
					.Feature("high").EqualToVariable("a")
					.Feature("back").EqualToVariable("b")
					.Feature("round").EqualToVariable("c").Value).Value;
			rightEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, vowel)
					.Feature("high").EqualToVariable("a")
					.Feature("back").EqualToVariable("b")
					.Feature("round").EqualToVariable("c").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "buifibuifi") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("buif?ibuif?i", output.Single().ToString());

			input = new Word(Allophonic, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buifibuifi", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, k)
					.Feature("asp").EqualToVariable("a").Value).Value;
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, g)
					.Feature("asp").EqualToVariable("a").Value).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "sagk") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]gk?", output.Single().ToString());

			input = new Word(Morphophonemic, "sag");
			var me = Assert.Throws<MorphException>(() => rule1.SynthesisRule.Apply(input, out output));
			Assert.AreEqual(MorphErrorCode.UninstantiatedFeature, me.ErrorCode);
		}

		[Test]
		public void EpenthesisRules()
		{
			var highVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var highFrontUnrndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Symbol("round-").Value;
			var highBackRndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back+").Symbol("round+").Value;
			var cons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Value;
			var vowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Value;
			var highBackRnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("high+").Symbol("back+").Symbol("round+").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Value;
			var rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs) {ApplicationMode = ApplicationMode.Simultaneous};
			var rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "buibui") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bui?bui?", output.Single().ToString());

			input = new Word(Allophonic, "bubui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buibuiii", input.ToString());

			input = new Word(Allophonic, "buibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buiiibui", input.ToString());

			input = new Word(Allophonic, "buibui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buiiibuiii", input.ToString());

			input = new Word(Allophonic, "bubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buibui", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs) {ApplicationMode = ApplicationMode.Simultaneous};
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table1.GetSymbolDefinition("i").FeatureStruct).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "biubiu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bi?ubi?u", output.Single().ToString());

			input = new Word(Allophonic, "bubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("biubiu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "ipʰit") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("i?(pʰ)it", output.Single().ToString());

			input = new Word(Allophonic, "pʰit");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("i(pʰ)it", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "pʰiti") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("(pʰ)iti?", output.Single().ToString());

			input = new Word(Allophonic, "pʰit");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("(pʰ)iti", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highBackRndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "biubiu") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bi?ubi?u", output.Single().ToString());

			input = new Word(Morphophonemic, "b+ubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("b+?iubiu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs) {ApplicationMode = ApplicationMode.Simultaneous};
			rhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, highVowel)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value).Value;
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, highVowel)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "biibuu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bii?buu?", output.Single().ToString());

			input = new Word(Allophonic, "bibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("biibuu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs) {ApplicationMode = ApplicationMode.Simultaneous};
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "biiibuii") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bii?i?bui?i?", output.Single().ToString());

			input = new Word(Allophonic, "bibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("biiibuii", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1 = new StandardPhonologicalRule("rule2", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highBackRnd).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highBackRndVowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Pattern<Word, ShapeNode>.New().Value;
			var rule2 = new StandardPhonologicalRule("rule3", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table1.GetSymbolDefinition("t").FeatureStruct).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "butubu") {Stratum = Allophonic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("but?[iuoyɯ]bu", output.Single().ToString());

			input = new Word(Allophonic, "buibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("butubu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs) {Direction = Direction.RightToLeft};
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "ipʰit") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("i?(pʰ)it", output.Single().ToString());

			input = new Word(Allophonic, "pʰit");
			var me = Assert.Throws<MorphException>(() => rule1.SynthesisRule.Apply(input, out output));
			Assert.AreEqual(MorphErrorCode.TooManySegs, me.ErrorCode);
		}

		[Test]
		public void DeletionRules()
		{
			var highFrontUnrndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("low-").Symbol("back-").Symbol("round-").Value;
			var highVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var cons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Value;
			var highBackRndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back+").Symbol("round+").Value;
			var asp = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("asp+").Value;
			var nonCons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Value;
			var vowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Value;
			var voiced = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("vd+").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			var rhs = Pattern<Word, ShapeNode>.New().Value;
			var leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bui?bui?", output.Single().ToString());

			input = new Word(Allophonic, "bubui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "buibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "buibui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs) {DelReapplications = 1};
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bui?i?i?bui?i?i?", output.Single().ToString());

			input = new Word(Allophonic, "bubui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "buibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "buibui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs) {DelReapplications = 1};
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("i?i?bui?i?bu", output.Single().ToString());

			input = new Word(Allophonic, "buibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "iibubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ibubu", input.ToString());

			input = new Word(Allophonic, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("i?i?bui?i?bu", output.Single().ToString());

			input = new Word(Allophonic, "buibu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buibu", input.ToString());

			input = new Word(Allophonic, "iibubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highBackRndVowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bui?i?bui?i?", output.Single().ToString());

			input = new Word(Allophonic, "bubui");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubui", input.ToString());

			input = new Word(Allophonic, "buibu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buibu", input.ToString());

			input = new Word(Allophonic, "buibui");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buibui", input.ToString());

			input = new Word(Allophonic, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("b").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.BoundaryType, Table3.GetSymbolDefinition("+").FeatureStruct).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("u").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("b").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("u").FeatureStruct).Value;
			var rule2 = new StandardPhonologicalRule("rule2", SpanFactory, lhs);
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.BoundaryType, Table3.GetSymbolDefinition("+").FeatureStruct).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("t").FeatureStruct).Value;
			var rule3 = new StandardPhonologicalRule("rule3", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, nonCons).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule3.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "b") {Stratum = Morphophonemic};
			Assert.IsFalse(rule3.AnalysisRule.Apply(input, out output));
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b?bu?b?u?", output.Single().ToString());

			input = new Word(Morphophonemic, "b+ubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsFalse(rule3.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("+?", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, cons)
					.Feature("poa").EqualToVariable("a")
					.Feature("vd").EqualToVariable("b")
					.Feature("cont").EqualToVariable("c")
					.Feature("nasal").EqualToVariable("d").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(Morpher.PhoneticFeatureSystem, cons)
					.Feature("poa").EqualToVariable("a")
					.Feature("vd").EqualToVariable("b")
					.Feature("cont").EqualToVariable("c")
					.Feature("nasal").EqualToVariable("d").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule2 = new StandardPhonologicalRule("rule2", SpanFactory, lhs);
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, voiced).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "aba") {Stratum = Morphophonemic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[a(a̘)][pb]?[pb][a(a̘)]", output.Single().ToString());

			input = new Word(Morphophonemic, "ab+ba");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("a+?ba", input.ToString());

			input = new Word(Morphophonemic, "abba");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("aba", input.ToString());
		}

		[Test]
		public void DisjunctiveRules()
		{
			var stop = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("cont-").Value;
			var asp = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("asp+").Value;
			var unasp = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("asp-").Value;
			var highVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back+").Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;
			var cons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Value;
			var highFrontVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Value;
			var frontRnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back-").Symbol("round+").Value;
			var frontRndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round+").Value;
			var backUnrnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back+").Symbol("round-").Value;
			var backUnrndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round-").Value;
			var frontUnrnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back-").Symbol("round-").Value;
			var frontUnrndVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round-").Value;
			var p = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("cont-").Symbol("vd-").Symbol("asp-").Symbol("bilabial").Value;
			var vd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("vd+").Value;
			var vowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Value;
			var voicelessStop = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("vd-").Symbol("cont-").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, stop).Value;
			var rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			
			var rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());
			
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "pʰip") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", output.Single().ToString());

			input = new Word(Allophonic, "pip");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("(pʰ)ip", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			
			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, backRndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, frontRnd).Value;
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, frontRndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backUnrnd).Value;
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, backUnrndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, frontUnrnd).Value;
			leftEnv = Pattern<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, frontUnrndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "bububu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bub[iuyɯ]b[iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, "bubibi");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bububu", input.ToString());

			input = new Word(Allophonic, "bubibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bububu", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, stop).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);

			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "pʰip");
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", output.Single().ToString());

			input = new Word(Allophonic, "pip");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			Assert.AreEqual("(pʰ)ip", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, p).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);

			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vd).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[p(pʰ)b]u", output.Single().ToString());

			input = new Word(Allophonic, "bupu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, voicelessStop).Value;
			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);

			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, voicelessStop).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Pattern<Word, ShapeNode>.New().Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "ktʰb") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[k(kʰ)][t(tʰ)]b", output.Single().ToString());

			input = new Word(Allophonic, "ktb");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("k(tʰ)b", input.ToString());
		}

		[Test]
		public void MultipleApplicationRules()
		{
			var highVowel = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("back+").Symbol("round+").Value;
			var i = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Symbol("round-").Value;
			var cons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("voc-").Value;

			var lhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs) {ApplicationMode = ApplicationMode.Simultaneous};
			var rhs = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, i).Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Pattern<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, "gigugu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("gig[iuyɯ]g[iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, "gigigi");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gigugu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, "gigugi") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("gig[iuyɯ]gi", output.Single().ToString());

			input = new Word(Allophonic, "gigigi");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gigugi", input.ToString());
		}
	}
}
