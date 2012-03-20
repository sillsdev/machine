using System.Linq;
using NUnit.Framework;
using SIL.Collections;
using SIL.HermitCrab;
using SIL.HermitCrab.PhonologicalRules;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace HermitCrabTest
{
	public class StandardPhonologicalRuleTest : HermitCrabTestBase
	{
		[Test]
		public void SimpleRules()
		{
			var asp = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("asp+").Value;
			var nonCons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-").Value;

			var rule1 = new RewriteRule("rule1") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("t")).Value};
			Allophonic.PhonologicalRules.Add(rule1);
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(nonCons).Value
			                   	});

			var rule2 = new RewriteRule("rule2") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("p")).Value};
			Allophonic.PhonologicalRules.Add(rule2);
			Morphophonemic.PhonologicalRules.Add(rule2);
			rule2.Subrules.Add(new RewriteSubrule
			              		{
			              			Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
			              			RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(nonCons).Value
			              		});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("pʰitʰ"), "1", "2");
			AssertMorphsEqual(morpher.ParseWord("datʰ"), "8", "9");
			AssertMorphsEqual(morpher.ParseWord("gab"), "11", "12");
		}

		[Test]
		public void LongDistanceRules()
		{
			var highVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-").Symbol("voc+")
				.Symbol("high+").Value;
			var backRnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back+")
				.Symbol("round+").Value;
			var rndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("round+").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Value;
			var lowVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("low+").Value;

			var rule3 = new RewriteRule("rule3") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value};
			Allophonic.PhonologicalRules.Add(rule3);
			rule3.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(backRnd).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(rndVowel).Annotation(cons).Annotation(lowVowel).Annotation(cons).Value
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bubabu"), "13", "14");

			rule3.Subrules.Clear();
			rule3.Subrules.Add(new RewriteSubrule
			                   	{
									Rhs = Pattern<Word, ShapeNode>.New().Annotation(backRnd).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(cons).Annotation(lowVowel).Annotation(cons).Annotation(rndVowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bubabu"), "13", "15");
		}

		[Test]
		public void AnchorRules()
		{
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-").Value;
			var vlUnasp = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("vd-")
				.Symbol("asp-").Value;
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+").Value;

			var rule3 = new RewriteRule("rule3") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(cons).Value};
			Allophonic.PhonologicalRules.Add(rule3);
			rule3.Subrules.Add(new RewriteSubrule
			                   	{
									Rhs = Pattern<Word, ShapeNode>.New().Annotation(vlUnasp).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(RightSideFS).Value
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("gap"), "10", "11", "12");

			rule3.Subrules.Clear();
			rule3.Subrules.Add(new RewriteSubrule
			                   	{
									Rhs = Pattern<Word, ShapeNode>.New().Annotation(vlUnasp).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Annotation(cons).Annotation(RightSideFS).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("kab"), "11", "12");

			rule3.Subrules.Clear();
			rule3.Subrules.Add(new RewriteSubrule
			                   	{
									Rhs = Pattern<Word, ShapeNode>.New().Annotation(vlUnasp).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(LeftSideFS).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("kab"), "11", "12");

			rule3.Subrules.Clear();
			rule3.Subrules.Add(new RewriteSubrule
			                   	{
									Rhs = Pattern<Word, ShapeNode>.New().Annotation(vlUnasp).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(LeftSideFS).Annotation(cons).Annotation(vowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("gap"), "10", "11", "12");
		}

		[Test]
		public void QuantifierRules()
		{
			var highVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+").Value;
			var backRnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back+")
				.Symbol("round+").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Value;
			var lowVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("low+").Value;
			var rndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment).Symbol("cons-")
				.Symbol("voc+")
				.Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("back+")
				.Symbol("round+").Value;

			var rule3 = new RewriteRule("rule3") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value};
			Allophonic.PhonologicalRules.Add(rule3);
			rule3.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(backRnd).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New()
										.Group(g => g.Annotation(cons).Annotation(lowVowel)).LazyRange(1, 2)
										.Annotation(cons).Annotation(rndVowel).Value
			                   	});

			var rule4 = new RewriteRule("rule4") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value};
			Allophonic.PhonologicalRules.Add(rule4);
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(backRnd).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(rndVowel)
										.Group(g => g.Annotation(cons).Annotation(lowVowel)).LazyRange(1, 2)
										.Annotation(cons).Value
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bubu"), "19");
			AssertMorphsEqual(morpher.ParseWord("bubabu"), "13", "14", "15");
			AssertMorphsEqual(morpher.ParseWord("bubababu"), "20", "21");
			Assert.IsFalse(morpher.ParseWord("bubabababu").Any());

			Allophonic.PhonologicalRules.Clear();

			var rule1 = new RewriteRule("rule1") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value};
			Allophonic.PhonologicalRules.Add(rule1);
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
									Rhs = Pattern<Word, ShapeNode>.New().Annotation(backRnd).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(backRndVowel).Annotation(highVowel).LazyRange(0, 2).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("buuubuuu"), "27");
		}

		[Test]
		public void MultipleSegmentRules()
		{
			var highVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+").Value;
			var backRnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back+")
				.Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("back+")
				.Symbol("round+").Value;
			var t = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("alveolar")
				.Symbol("del_rel-")
				.Symbol("asp-")
				.Symbol("vd-")
				.Symbol("strident-").Value;

			var rule1 = new RewriteRule("rule1") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Annotation(highVowel).Value};
			Allophonic.PhonologicalRules.Add(rule1);
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(backRnd).Annotation(backRnd).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(backRndVowel).Value
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("buuubuuu"), "27");

			var rule2 = new RewriteRule("rule2") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(t).Value};
			Allophonic.PhonologicalRules.Add(rule2);
			rule2.Subrules.Add(new RewriteSubrule
			                   	{
			                   		RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(backRndVowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("buuubuuu"), "27");
		}

		[Test]
		public void BoundaryRules()
		{
			var highVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+").Value;
			var backRndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("back+")
				.Symbol("round+").Value;
			var backRnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back+")
				.Symbol("round+").Value;
			var unbackUnrnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back-")
				.Symbol("round-").Value;
			var unbackUnrndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("back-")
				.Symbol("round-").Value;
			var backVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("back+").Value;
			var unrndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("round-").Value;
			var lowBack = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back+")
				.Symbol("low+")
				.Symbol("high-").Value;
			var bilabialCons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-")
				.Symbol("bilabial").Value;
			var unvdUnasp = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("vd-")
				.Symbol("asp-").Value;
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-").Value;
			var asp = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("asp+").Value;

			var rule1 = new RewriteRule("rule1") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value};
			Morphophonemic.PhonologicalRules.Add(rule1);
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(backRnd).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(backRndVowel).Annotation(Table3.GetSymbolFeatureStruct("+")).Value
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("buub"), "30");

			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(unbackUnrnd).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("+")).Annotation(unbackUnrndVowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("biib"), "30");

			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(backRnd).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(backRndVowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("buub"), "30", "31");

			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(unbackUnrnd).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(unbackUnrndVowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("biib"), "30", "31");

			rule1.Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("i")).Value;
			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("b")).Value
			                   	});

			var rule2 = new RewriteRule("rule2") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(backVowel).Value};
			Morphophonemic.PhonologicalRules.Add(rule2);
			rule2.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("a")).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(Table3.GetSymbolFeatureStruct("+"))
										.Annotation(Table3.GetSymbolFeatureStruct("b")).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bab"), "30");

			rule1.Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("u")).Value;
			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("b")).Value
			                   	});

			rule2.Lhs = Pattern<Word, ShapeNode>.New().Annotation(unrndVowel).Value;
			rule2.Subrules.Clear();
			rule2.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(lowBack).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(Table3.GetSymbolFeatureStruct("b"))
										.Annotation(Table3.GetSymbolFeatureStruct("+")).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bab"), "30");

			Morphophonemic.PhonologicalRules.Remove(rule2);

			rule1.Lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(bilabialCons)
				.Annotation(Table3.GetSymbolFeatureStruct("+"))
				.Annotation(bilabialCons).Value;
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New()
										.Annotation(unvdUnasp)
										.Annotation(Table3.GetSymbolFeatureStruct("+"))
										.Annotation(unvdUnasp).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("appa"), "39");

			rule1.Lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(bilabialCons)
				.Annotation(bilabialCons).Value;
			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New()
										.Annotation(unvdUnasp)
										.Annotation(unvdUnasp).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("appa"), "40");

			rule1.Lhs = Pattern<Word, ShapeNode>.New().Annotation(cons).Value;
			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("+")).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			Assert.IsFalse(morpher.ParseWord("pʰipʰ").Any());

			Morphophonemic.PhonologicalRules.Clear();

			rule1 = new RewriteRule("rule1");
			Allophonic.PhonologicalRules.Add(rule1);
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New()
										.Annotation(Table1.GetSymbolFeatureStruct("t"))
										.Annotation(Table1.GetSymbolFeatureStruct("a")).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(LeftSideFS).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(cons).Annotation(vowel).Annotation(RightSideFS).Value,
									RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("taba"), "pos2");
			AssertMorphsEqual(morpher.ParseWord("ba"), "pos1");

			RewriteSubrule subrule = rule1.Subrules[0];
			subrule.RequiredSyntacticFeatureStruct.Clear();
			subrule.RequiredMprFeatures.Add(Latinate);

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("taba"), "pos1");

			subrule.RequiredMprFeatures.Clear();
			subrule.ExcludedMprFeatures.Add(Latinate);

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("taba"), "pos2");
		}

		[Test]
		public void CommonFeatureRules()
		{
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+").Value;
			var vdLabFric = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("labiodental")
				.Symbol("vd+")
				.Symbol("strident+")
				.Symbol("cont+").Value;

			var rule1 = new RewriteRule("rule1") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("p")).Value};
			Allophonic.PhonologicalRules.Add(rule1);
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(vdLabFric).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("buvu"), "46");

			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("v")).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("buvu"), "46");
		}

		[Test]
		public void AlphaVariableRules()
		{
			var highVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-").Value;
			var nasalCons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-")
				.Symbol("nasal+").Value;
			var voicelessStop = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("vd-")
				.Symbol("cont-").Value;
			var asp = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("asp+").Value;
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+").Value;
			var unasp = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("asp-").Value;
			var k = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-")
				.Symbol("velar")
				.Symbol("vd-")
				.Symbol("cont-")
				.Symbol("nasal-").Value;
			var g = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-")
				.Symbol("velar")
				.Symbol("vd+")
				.Symbol("cont-")
				.Symbol("nasal-").Value;

			var rule1 = new RewriteRule("rule1") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value};
			Morphophonemic.PhonologicalRules.Add(rule1);
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem)
											.Symbol(HCFeatureSystem.Segment)
											.Feature("back").EqualToVariable("a")
											.Feature("round").EqualToVariable("b").Value).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, highVowel)
											.Feature("back").EqualToVariable("a")
											.Feature("round").EqualToVariable("b").Value)
										.Annotation(cons).Value
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bububu"), "42", "43");

			rule1.Lhs = Pattern<Word, ShapeNode>.New().Annotation(nasalCons).Value;
			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem)
											.Symbol(HCFeatureSystem.Segment)
											.Feature("poa").EqualToVariable("a").Value).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, cons)
											.Feature("poa").EqualToVariable("a").Value).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("mbindiŋg"), "45");

			Morphophonemic.PhonologicalRules.Clear();
			Allophonic.PhonologicalRules.Add(rule1);

			rule1.Lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, voicelessStop)
					.Feature("poa").EqualToVariable("a").Value).Value;
			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, voicelessStop)
											.Feature("poa").EqualToVariable("a").Value)
										.Annotation(vowel).Value
			                   	});
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(unasp).Value,
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("pipʰ"), "41");

			rule1.Lhs = Pattern<Word, ShapeNode>.New().Value;
			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("f")).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, vowel)
											.Feature("high").EqualToVariable("a")
											.Feature("back").EqualToVariable("b")
											.Feature("round").EqualToVariable("c").Value).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, vowel)
											.Feature("high").EqualToVariable("a")
											.Feature("back").EqualToVariable("b")
											.Feature("round").EqualToVariable("c").Value).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("buifibuifi"), "27");

			Allophonic.PhonologicalRules.Clear();
			Morphophonemic.PhonologicalRules.Add(rule1);

			rule1.Subrules.Clear();
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, k)
											.Feature("asp").EqualToVariable("a").Value).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, g)
											.Feature("asp").EqualToVariable("a").Value).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(RightSideFS).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			var me = Assert.Throws<MorphException>(() => morpher.ParseWord("sagk"));
			Assert.AreEqual(MorphErrorCode.UninstantiatedFeature, me.ErrorCode);
		}

		[Test]
		public void EpenthesisRules()
		{
			var highVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+").Value;
			var highFrontUnrndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+")
				.Symbol("back-")
				.Symbol("round-").Value;
			var highBackRndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+")
				.Symbol("back+")
				.Symbol("round+").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-").Value;
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+").Value;
			var highBackRnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("high+")
				.Symbol("back+")
				.Symbol("round+").Value;

			var rule4 = new RewriteRule("rule4") {ApplicationMode = ApplicationMode.Simultaneous};
			Allophonic.PhonologicalRules.Add(rule4);
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(highFrontUnrndVowel).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("buibui"), "19");

			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("i")).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("biubiu"), "19");

			rule4.ApplicationMode = ApplicationMode.Iterative;
			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(highFrontUnrndVowel).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(LeftSideFS).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(cons).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("ipʰit"), "1");

			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
								{
									Rhs = Pattern<Word, ShapeNode>.New().Annotation(highFrontUnrndVowel).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(cons).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(RightSideFS).Value
								});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("pʰiti"), "1");

			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
								{
									Rhs = Pattern<Word, ShapeNode>.New().Annotation(highFrontUnrndVowel).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(cons).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(highBackRndVowel).Value
								});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("biubiu"), "19");

			rule4.ApplicationMode = ApplicationMode.Simultaneous;
			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, highVowel)
											.Feature("back").EqualToVariable("a")
											.Feature("round").EqualToVariable("b").Value).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, highVowel)
											.Feature("back").EqualToVariable("a")
											.Feature("round").EqualToVariable("b").Value).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("biibuu"), "18");

			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(highFrontUnrndVowel).Annotation(highFrontUnrndVowel).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("biiibuii"), "18");

			rule4.ApplicationMode = ApplicationMode.Iterative;
			rule4.Direction = Direction.RightToLeft;
			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
									Rhs = Pattern<Word, ShapeNode>.New().Annotation(highFrontUnrndVowel).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(LeftSideFS).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			var me = Assert.Throws<MorphException>(() => morpher.ParseWord("ipʰit"));
			Assert.AreEqual(MorphErrorCode.TooManySegs, me.ErrorCode);

			Allophonic.PhonologicalRules.Clear();

			var rule1 = new RewriteRule("rule1") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value};
			Allophonic.PhonologicalRules.Add(rule1);
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(highBackRnd).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(highBackRndVowel).Value
			                   	});

			var rule2 = new RewriteRule("rule2");
			Allophonic.PhonologicalRules.Add(rule2);
			rule2.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("t")).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("butubu"), "25");
		}

		[Test]
		public void DeletionRules()
		{
			var highFrontUnrndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+")
				.Symbol("low-")
				.Symbol("back-")
				.Symbol("round-").Value;
			var highVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-").Value;
			var highBackRndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+")
				.Symbol("back+")
				.Symbol("round+").Value;
			var asp = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("asp+").Value;
			var nonCons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-").Value;
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+").Value;
			var voiced = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("vd+").Value;

			var rule4 = new RewriteRule("rule4") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(highFrontUnrndVowel).Value};
			Allophonic.PhonologicalRules.Add(rule4);
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
			                   		LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bubu"), "24", "25", "26", "19");

			rule4.DelReapplications = 1;

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bubu"), "24", "25", "26", "27", "19");

			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
			                   		RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(cons).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bubu"), "25", "19");

			rule4.DelReapplications = 0;
			rule4.Lhs = Pattern<Word, ShapeNode>.New().Annotation(highFrontUnrndVowel).Annotation(highFrontUnrndVowel).Value;

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bubu"), "29", "19");

			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(highBackRndVowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bubu"), "27", "19");

			Allophonic.PhonologicalRules.Clear();
			Morphophonemic.PhonologicalRules.Add(rule4);

			rule4.Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("b")).Value;
			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
			                   		LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(LeftSideFS).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("+")).Value
			                   	});

			var rule5 = new RewriteRule("rule5")
			            	{
								Lhs = Pattern<Word, ShapeNode>.New()
									.Annotation(Table3.GetSymbolFeatureStruct("u"))
									.Annotation(Table3.GetSymbolFeatureStruct("b"))
									.Annotation(Table3.GetSymbolFeatureStruct("u")).Value
			            	};
			Morphophonemic.PhonologicalRules.Add(rule5);
			rule5.Subrules.Add(new RewriteSubrule
			                   	{
			                   		LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("+")).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(RightSideFS).Value
			                   	});

			var rule1 = new RewriteRule("rule1") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("t")).Value};
			Morphophonemic.PhonologicalRules.Add(rule1);
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(nonCons).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			Assert.IsFalse(morpher.ParseWord("b").Any());

			Morphophonemic.PhonologicalRules.Clear();
			Allophonic.PhonologicalRules.Add(rule4);
			Allophonic.PhonologicalRules.Add(rule5);
			Allophonic.PhonologicalRules.Add(rule1);

			rule4.Subrules[0].LeftEnvironment.Children.Clear();

			rule5.Lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(Table3.GetSymbolFeatureStruct("u"))
				.Annotation(Table3.GetSymbolFeatureStruct("b"))
				.Annotation(Table3.GetSymbolFeatureStruct("i")).Value;
			rule5.Subrules[0].RightEnvironment.Children.Clear();

			morpher = new Morpher(SpanFactory, Language);
			Assert.IsFalse(morpher.ParseWord("b").Any());

			Allophonic.PhonologicalRules.Clear();
			Allophonic.PhonologicalRules.Add(rule4);
			Morphophonemic.PhonologicalRules.Add(rule5);

			rule4.Lhs = Pattern<Word, ShapeNode>.New()
				.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, cons)
					.Feature("poa").EqualToVariable("a")
					.Feature("vd").EqualToVariable("b")
					.Feature("cont").EqualToVariable("c")
					.Feature("nasal").EqualToVariable("d").Value).Value;
			rule4.Subrules.Clear();
			rule4.Subrules.Add(new RewriteSubrule
			                   	{
			                   		RightEnvironment = Pattern<Word, ShapeNode>.New()
										.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, cons)
											.Feature("poa").EqualToVariable("a")
											.Feature("vd").EqualToVariable("b")
											.Feature("cont").EqualToVariable("c")
											.Feature("nasal").EqualToVariable("d").Value).Value
			                   	});

			rule5.Lhs = Pattern<Word, ShapeNode>.New().Annotation(cons).Value;
			rule5.Subrules.Clear();
			rule5.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(voiced).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value,
									RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
			                   	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("aba"), "39", "40");
		}

		[Test]
		public void DisjunctiveRules()
		{
			var stop = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("cont-").Value;
			var asp = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("asp+").Value;
			var unasp = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("asp-").Value;
			var highVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+").Value;
			var backRnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back+")
				.Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("back+")
				.Symbol("round+").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-").Value;
			var highFrontVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+")
				.Symbol("back-").Value;
			var frontRnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back-")
				.Symbol("round+").Value;
			var frontRndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("back-")
				.Symbol("round+").Value;
			var backUnrnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back+")
				.Symbol("round-").Value;
			var backUnrndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("back+")
				.Symbol("round-").Value;
			var frontUnrnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back-")
				.Symbol("round-").Value;
			var frontUnrndVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("back-")
				.Symbol("round-").Value;
			var p = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("cont-")
				.Symbol("vd-")
				.Symbol("asp-")
				.Symbol("bilabial").Value;
			var vd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("vd+").Value;
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+").Value;
			var voicelessStop = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("vd-")
				.Symbol("cont-").Value;

			var disrule1 = new RewriteRule("disrule1") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(stop).Value};
			Allophonic.PhonologicalRules.Add(disrule1);
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(LeftSideFS).Value
			                      	});
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(unasp).Value
			                      	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("pʰip"), "41");

			disrule1.Lhs = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value;
			disrule1.Subrules.Clear();
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(backRnd).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New()
											.Annotation(backRndVowel)
											.Group(g => g.Annotation(cons).Annotation(highFrontVowel)).LazyZeroOrMore
											.Annotation(cons).Value
			                      	});
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(frontRnd).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New()
											.Annotation(frontRndVowel)
											.Group(g => g.Annotation(cons).Annotation(highFrontVowel)).LazyZeroOrMore
											.Annotation(cons).Value
			                      	});
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(backUnrnd).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New()
											.Annotation(backUnrndVowel)
											.Group(g => g.Annotation(cons).Annotation(highFrontVowel)).LazyZeroOrMore
											.Annotation(cons).Value
			                      	});
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(frontUnrnd).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New()
											.Annotation(frontUnrndVowel)
											.Group(g => g.Annotation(cons).Annotation(highFrontVowel)).LazyZeroOrMore
											.Annotation(cons).Value
			                      	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bububu"), "42", "43");

			disrule1.Lhs = Pattern<Word, ShapeNode>.New().Annotation(stop).Value;
			disrule1.Subrules.Clear();
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(LeftSideFS).Value
			                      	});
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(unasp).Value,
										RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(RightSideFS).Value
			                      	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("pʰip"), "41");

			disrule1.Lhs = Pattern<Word, ShapeNode>.New().Annotation(p).Value;
			disrule1.Subrules.Clear();
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(vd).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
			                      	});
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
										RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(RightSideFS).Value
			                      	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bubu"), "46", "19");

			disrule1.Lhs = Pattern<Word, ShapeNode>.New().Annotation(voicelessStop).Value;
			disrule1.Subrules.Clear();
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(voicelessStop).Value
			                      	});
			disrule1.Subrules.Add(new RewriteSubrule
			                      	{
			                      		Rhs = Pattern<Word, ShapeNode>.New().Annotation(unasp).Value
			                      	});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("ktʰb"), "49");
		}

		[Test]
		public void MultipleApplicationRules()
		{
			var highVowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+").Value;
			var backRnd = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("back+")
				.Symbol("round+").Value;
			var i = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high+")
				.Symbol("back-")
				.Symbol("round-").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("voc-").Value;

			var rule1 = new RewriteRule("rule1") { ApplicationMode = ApplicationMode.Simultaneous, Lhs = Pattern<Word, ShapeNode>.New().Annotation(highVowel).Value };
			Allophonic.PhonologicalRules.Add(rule1);
			rule1.Subrules.Add(new RewriteSubrule
			                   	{
			                   		Rhs = Pattern<Word, ShapeNode>.New().Annotation(backRnd).Value,
									LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(i).Annotation(cons).Value
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("gigugu"), "44");

			rule1.ApplicationMode = ApplicationMode.Iterative;

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("gigugi"), "44");
		}
	}
}
