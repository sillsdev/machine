using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;
using SIL.HermitCrab;

namespace HermitCrabTest
{
	public class StandardPhonologicalRuleTest : HermitCrabTestBase
	{
		[Test]
		public void SimpleRules()
		{
			var asp = FeatureStruct.New(FeatSys).Symbol("asp+").Value;
			var nonCons = FeatureStruct.New(FeatSys).Symbol("cons-").Value;

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table1.GetSegmentDefinition("t").FeatureStruct).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, nonCons).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("p").FeatureStruct).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, nonCons).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "pʰitʰ") {Stratum = Allophonic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[p(pʰ)]i[t(tʰ)]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "pʰit");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("(pʰ)i(tʰ)", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "pit");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsTrue(rule2.SynthesisRule.Apply(output.Single(), out output));
			Word result = output.Single();
			result.Stratum = Surface;
			Assert.AreEqual("(pʰ)i(tʰ)", result.ToString());

			input = new Word(Surface, Mode.Analysis, "datʰ") {Stratum = Allophonic};
			Assert.IsFalse(rule2.AnalysisRule.Apply(input, out output));
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("da[t(tʰ)]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "dat");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("da(tʰ)", input.ToString());

			input = new Word(Surface, Mode.Analysis, "gab") {Stratum = Allophonic};
			Assert.IsFalse(rule2.AnalysisRule.Apply(input, out output));
			Assert.IsFalse(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("gab", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "gab");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("gab", input.ToString());
		}

		[Test]
		public void LongDistanceRules()
		{
			var highVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(FeatSys).Symbol("back+").Symbol("round+").Value;
			var rndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Value;
			var cons = FeatureStruct.New(FeatSys).Symbol("cons+").Value;
			var lowVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Value;

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule = new StandardPhonologicalRule("rule", "rule", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, rndVowel)
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.SegmentType, lowVowel)
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());
			
			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "bubabu") {Stratum = Allophonic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bubab[iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubabu");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubabi");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			rule = new StandardPhonologicalRule("rule", "rule", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.SegmentType, lowVowel)
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.SegmentType, rndVowel).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "bubabu") {Stratum = Allophonic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b[iuyɯ]babu", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubabu");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bɯbabu");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());
		}

		[Test]
		public void AnchorRules()
		{
			var cons = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Value;
			var vlUnasp = FeatureStruct.New(FeatSys).Symbol("vd-").Symbol("asp-").Value;
			var vowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Value;

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rule = new StandardPhonologicalRule("rule", "rule", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vlUnasp).Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "gap") {Stratum = Morphophonemic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("g[a(a̘)][pb]", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "ga̘p");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gap", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "gab");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gap", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "ga+b");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ga+?p", input.ToString());

			rule = new StandardPhonologicalRule("rule", "rule", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rightEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, vowel)
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "kab") {Stratum = Morphophonemic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[kg][a(a̘)]b", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "gab");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("kab", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "ga+b");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ka+?b", input.ToString());

			rule = new StandardPhonologicalRule("rule", "rule", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "kab") {Stratum = Morphophonemic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[kg][a(a̘)]b", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "gab");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("kab", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "ga+b");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ka+?b", input.ToString());

			rule = new StandardPhonologicalRule("rule", "rule", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.AnchorType, LeftSideFS)
				.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "gap") {Stratum = Morphophonemic};
			Assert.IsTrue(rule.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("g[a(a̘)][pb]", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "ga̘p");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gap", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "gab");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gap", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "ga+b");
			Assert.IsTrue(rule.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ga+?p", input.ToString());
		}

		[Test]
		public void QuantifierRules()
		{
			var highVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(FeatSys).Symbol("back+").Symbol("round+").Value;
			var cons = FeatureStruct.New(FeatSys).Symbol("cons+").Value;
			var lowVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Value;
			var rndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Value;
			var rightEnv = Expression<Word, ShapeNode>.New()
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, lowVowel)).LazyRange(1, 2)
				.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, rndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var rule2 = new StandardPhonologicalRule("rule2", "rule2", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, rndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, lowVowel)).LazyRange(1, 2)
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "bubu") {Stratum = Morphophonemic};
			Assert.IsFalse(rule2.AnalysisRule.Apply(input, out output));
			Assert.IsFalse(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "b+ubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("b+?ubu", input.ToString());

			input = new Word(Surface, Mode.Analysis, "bubabu") {Stratum = Allophonic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b[iuyɯ]bab[iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubabu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubabi");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bɯbabu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubabu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bibabi");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bibabi", input.ToString());

			input = new Word(Surface, Mode.Analysis, "bubababu") {Stratum = Allophonic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b[iuyɯ]babab[iuyɯ]", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubababi");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubababu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bibababu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubababu", input.ToString());

			input = new Word(Surface, Mode.Analysis, "bubabababu") {Stratum = Allophonic};
			Assert.IsFalse(rule2.AnalysisRule.Apply(input, out output));
			Assert.IsFalse(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bubabababu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRndVowel).Annotation(HCFeatureSystem.SegmentType, highVowel).LazyRange(0, 2).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "buuubuuu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[iuyɯ][iuyɯ]bu[iuyɯ][iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buuubuuu", input.ToString());
		}

		[Test]
		public void MultipleSegmentRules()
		{
			var highVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(FeatSys).Symbol("back+").Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;
			var t = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("alveolar").Symbol("del_rel-").Symbol("asp-").Symbol("vd-").Symbol("strident-").Value;

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRndVowel).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "buuubuuu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[iuyɯ][iuyɯ]bu[iuyɯ][iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buuubuuu", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, t).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRndVowel).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "buuubuuu") {Stratum = Allophonic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("but?[iuyɯ]t?[iuyɯ]but?[iuyɯ]t?[iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buuubuuu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buitibuiti");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buitibuiti", input.ToString());
		}

		[Test]
		public void BoundaryRules()
		{
			var highVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;
			var backRnd = FeatureStruct.New(FeatSys).Symbol("back+").Symbol("round+").Value;
			var unbackUnrnd = FeatureStruct.New(FeatSys).Symbol("back-").Symbol("round-").Value;
			var unbackUnrndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round-").Value;
			var backVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Value;
			var unrndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("round-").Value;
			var lowBack = FeatureStruct.New(FeatSys).Symbol("back+").Symbol("low+").Symbol("high-").Value;
			var bilabialCons = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Symbol("bilabial").Value;
			var unvdUnasp = FeatureStruct.New(FeatSys).Symbol("vd-").Symbol("asp-").Value;
			var vowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Value;
			var cons = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Value;
			var asp = FeatureStruct.New(FeatSys).Symbol("asp+").Value;

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, backRndVowel)
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "buub") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[iuyɯ]b", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bu+?ub", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "buib");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buib", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unbackUnrnd).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, unbackUnrndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "biib") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b[iuyɯ]ib", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bi+?ib", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "buib");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buib", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRndVowel).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "buub") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[iuyɯ]b", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bu+?ub", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "buib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buub", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unbackUnrnd).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unbackUnrndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "biib") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b[iuyɯ]ib", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bi+?ib", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "buib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("biib", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("i").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("b").FeatureStruct).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backVowel).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("a").FeatureStruct).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("b").FeatureStruct).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "bab") {Stratum = Morphophonemic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("i?b[a(a̘)uɯo]i?b", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ba+?b", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "buib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bub", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("u").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("b").FeatureStruct).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unrndVowel).Value;
			rule2 = new StandardPhonologicalRule("rule2", "rule2", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, lowBack).Value;
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("b").FeatureStruct)
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "bab") {Stratum = Morphophonemic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu?[a(a̘)iɯ]bu?", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "bu+ib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("b+?ab", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "buib");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsFalse(rule2.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bib", input.ToString());

			lhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons)
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp)
				.Annotation(HCFeatureSystem.BoundaryType, Table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "appa") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[a(a̘)][pb][pb][a(a̘)]", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "ab+ba");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ap+?pa", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "abba");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("abba", input.ToString());

			lhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons)
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp)
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "appa") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[a(a̘)][pb][pb][a(a̘)]", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "ab+ba");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("ab+?ba", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "abba");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("appa", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.BoundaryType, Table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "pʰipʰ") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "pip");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("pip", input.ToString());
		}

		[Test]
		public void CommonFeatureRules()
		{
			var vowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Value;
			var vdLabFric = FeatureStruct.New(FeatSys).Symbol("labiodental").Symbol("vd+").Symbol("strident+").Symbol("cont+").Value;

			var lhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, Table1.GetSegmentDefinition("p").FeatureStruct).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vdLabFric).Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "buvu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[pbmfv]u", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bupu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buvu", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "b+ubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("b+?ubu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table1.GetSegmentDefinition("v").FeatureStruct).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "buvu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[pbmfv]u", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bupu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buvu", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "b+ubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("b+?ubu", input.ToString());
		}

		[Test]
		public void AlphaVariableRules()
		{
			var highVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var cons = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Value;
			var nasalCons = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Symbol("nasal+").Value;
			var voicelessStop = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("vd-").Symbol("cont-").Value;
			var asp = FeatureStruct.New(FeatSys).Symbol("asp+").Value;
			var vowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Value;
			var unasp = FeatureStruct.New(FeatSys).Symbol("asp-").Value;
			var k = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Symbol("velar").Symbol("vd-").Symbol("cont-").Symbol("nasal-").Value;
			var g = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Symbol("velar").Symbol("vd+").Symbol("cont-").Symbol("nasal-").Value;

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			var rhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value).Value;
			var leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, highVowel)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value)
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "bububu") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bub[iuyɯ]b[iuyɯ]", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "bubibi");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bububu", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "bubibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bububu", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, nasalCons).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys)
					.Feature("poa").EqualToVariable("a").Value).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, cons)
					.Feature("poa").EqualToVariable("a").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "mbindiŋg") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[mnŋ]bi[mnŋ]di[mnŋ]g", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "nbinding");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("mbindiŋg", input.ToString());

			lhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, voicelessStop)
					.Feature("poa").EqualToVariable("a").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, voicelessStop)
					.Feature("poa").EqualToVariable("a").Value)
				.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "pipʰ") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "pip");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("pi(pʰ)", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table1.GetSegmentDefinition("f").FeatureStruct).Value;
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, vowel)
					.Feature("high").EqualToVariable("a")
					.Feature("back").EqualToVariable("b")
					.Feature("round").EqualToVariable("c").Value).Value;
			rightEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, vowel)
					.Feature("high").EqualToVariable("a")
					.Feature("back").EqualToVariable("b")
					.Feature("round").EqualToVariable("c").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "buifibuifi") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("buif?ibuif?i", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buifibuifi", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, k)
					.Feature("asp").EqualToVariable("a").Value).Value;
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, g)
					.Feature("asp").EqualToVariable("a").Value).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "sagk") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]gk?", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "sag");
			var me = Assert.Throws<MorphException>(() => rule1.SynthesisRule.Apply(input, out output));
			Assert.AreEqual(MorphErrorCode.UninstantiatedFeature, me.ErrorCode);
		}

		[Test]
		public void EpenthesisRules()
		{
			var highVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var highFrontUnrndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Symbol("round-").Value;
			var highBackRndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back+").Symbol("round+").Value;
			var cons = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Value;
			var vowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Value;
			var highBackRnd = FeatureStruct.New(FeatSys).Symbol("high+").Symbol("back+").Symbol("round+").Value;

			var lhs = Expression<Word, ShapeNode>.New().Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Simultaneous, lhs);
			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "buibui") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bui?bui?", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buibuiii", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buiiibui", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buiiibuiii", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("buibui", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Simultaneous, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table1.GetSegmentDefinition("i").FeatureStruct).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "biubiu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bi?ubi?u", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("biubiu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "ipʰit") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("i?(pʰ)it", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "pʰit");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("i(pʰ)it", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "pʰiti") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("(pʰ)iti?", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "pʰit");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("(pʰ)iti", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highBackRndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "biubiu") {Stratum = Morphophonemic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bi?ubi?u", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "b+ubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("b+?iubiu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Simultaneous, lhs);
			rhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, highVowel)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value).Value;
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, highVowel)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "biibuu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bii?buu?", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("biibuu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Simultaneous, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "biiibuii") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bii?i?bui?i?", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("biiibuii", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1 = new StandardPhonologicalRule("rule2", "rule2", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highBackRnd).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highBackRndVowel).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<Word, ShapeNode>.New().Value;
			var rule2 = new StandardPhonologicalRule("rule3", "rule3", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table1.GetSegmentDefinition("t").FeatureStruct).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "butubu") {Stratum = Allophonic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("but?[iuoyɯ]bu", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("butubu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.RightToLeft, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "ipʰit") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("i?(pʰ)it", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "pʰit");
			var me = Assert.Throws<MorphException>(() => rule1.SynthesisRule.Apply(input, out output));
			Assert.AreEqual(MorphErrorCode.TooManySegs, me.ErrorCode);
		}

		[Test]
		public void DeletionRules()
		{
			var highFrontUnrndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("low-").Symbol("back-").Symbol("round-").Value;
			var highVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var cons = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Value;
			var highBackRndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back+").Symbol("round+").Value;
			var asp = FeatureStruct.New(FeatSys).Symbol("asp+").Value;
			var nonCons = FeatureStruct.New(FeatSys).Symbol("cons-").Value;
			var vowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Value;
			var voiced = FeatureStruct.New(FeatSys).Symbol("vd+").Value;

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			var rhs = Expression<Word, ShapeNode>.New().Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bui?bui?", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 1, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bui?i?i?bui?i?i?", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibui");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 1, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("i?i?bui?i?bu", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "iibubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("ibubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("i?i?bui?i?bu", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buibu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "iibubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highBackRndVowel).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bui?i?bui?i?", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubui");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubui", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buibu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buibui");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("buibui", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "buiibuii");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("b").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.BoundaryType, Table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("u").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("b").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("u").FeatureStruct).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.BoundaryType, Table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("t").FeatureStruct).Value;
			var rule3 = new StandardPhonologicalRule("rule3", "rule3", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, nonCons).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule3.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "b") {Stratum = Morphophonemic};
			Assert.IsFalse(rule3.AnalysisRule.Apply(input, out output));
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("b?bu?b?u?", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "b+ubu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsFalse(rule3.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("+?", input.ToString());

			lhs = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, cons)
					.Feature("poa").EqualToVariable("a")
					.Feature("vd").EqualToVariable("b")
					.Feature("cont").EqualToVariable("c")
					.Feature("nasal").EqualToVariable("d").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(FeatSys, cons)
					.Feature("poa").EqualToVariable("a")
					.Feature("vd").EqualToVariable("b")
					.Feature("cont").EqualToVariable("c")
					.Feature("nasal").EqualToVariable("d").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule2 = new StandardPhonologicalRule("rule2", "rule2", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, voiced).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "aba") {Stratum = Morphophonemic};
			Assert.IsTrue(rule2.AnalysisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[a(a̘)][pb]?[pb][a(a̘)]", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "ab+ba");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			Assert.IsTrue(rule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("a+?ba", input.ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "abba");
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
			var stop = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("cont-").Value;
			var asp = FeatureStruct.New(FeatSys).Symbol("asp+").Value;
			var unasp = FeatureStruct.New(FeatSys).Symbol("asp-").Value;
			var highVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(FeatSys).Symbol("back+").Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;
			var cons = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Value;
			var highFrontVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Value;
			var frontRnd = FeatureStruct.New(FeatSys).Symbol("back-").Symbol("round+").Value;
			var frontRndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round+").Value;
			var backUnrnd = FeatureStruct.New(FeatSys).Symbol("back+").Symbol("round-").Value;
			var backUnrndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round-").Value;
			var frontUnrnd = FeatureStruct.New(FeatSys).Symbol("back-").Symbol("round-").Value;
			var frontUnrndVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round-").Value;
			var p = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("cont-").Symbol("vd-").Symbol("asp-").Symbol("bilabial").Value;
			var vd = FeatureStruct.New(FeatSys).Symbol("vd+").Value;
			var vowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Value;
			var voicelessStop = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("vd-").Symbol("cont-").Value;

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, stop).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			
			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());
			
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "pʰip") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "pip");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("(pʰ)ip", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			
			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, backRndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, frontRnd).Value;
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, frontRndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backUnrnd).Value;
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, backUnrndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, frontUnrnd).Value;
			leftEnv = Expression<Word, ShapeNode>.New()
				.Annotation(HCFeatureSystem.SegmentType, frontUnrndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "bububu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bub[iuyɯ]b[iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubibi");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bububu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubibu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bububu", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, stop).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);

			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, LeftSideFS).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "pʰip");
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "pip");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			Assert.AreEqual("(pʰ)ip", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, p).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);

			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vd).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.AnchorType, RightSideFS).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "bubu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("bu[p(pʰ)b]u", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bupu");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			input = new Word(Allophonic, Mode.Synthesis, "bubu");
			Assert.IsFalse(rule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("bubu", input.ToString());

			lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, voicelessStop).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);

			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, voicelessStop).Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Expression<Word, ShapeNode>.New().Value;
			rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "ktʰb") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("[k(kʰ)][t(tʰ)]b", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "ktb");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("k(tʰ)b", input.ToString());
		}

		[Test]
		public void MultipleApplicationRules()
		{
			var highVowel = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(FeatSys).Symbol("back+").Symbol("round+").Value;
			var i = FeatureStruct.New(FeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Symbol("round-").Value;
			var cons = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("voc-").Value;

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Simultaneous, lhs);
			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, i).Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "gigugu") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("gig[iuyɯ]g[iuyɯ]", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "gigigi");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gigugu", input.ToString());

			rule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			input = new Word(Surface, Mode.Analysis, "gigugi") {Stratum = Allophonic};
			Assert.IsTrue(rule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("gig[iuyɯ]gi", output.Single().ToString());

			input = new Word(Allophonic, Mode.Synthesis, "gigigi");
			Assert.IsTrue(rule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Surface;
			Assert.AreEqual("gigugi", input.ToString());
		}
	}
}
