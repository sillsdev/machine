using NUnit.Framework;
using SIL.HermitCrab.MorphologicalRules;
using SIL.HermitCrab.PhonologicalRules;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.Tests.PhonologicalRules
{
	public class MetathesisRuleTests : HermitCrabTestBase
	{
		[Test]
		public void SimpleRule()
		{
			var rule1 = new MetathesisRule
			            	{
								Name = "rule1",
			            		Pattern = Pattern<Word, ShapeNode>.New()
			            			.Group("1", group => group.Annotation(Table3.GetSymbolFeatureStruct("i")))
			            			.Group("2", group => group.Annotation(Table3.GetSymbolFeatureStruct("u"))).Value,
			            		LeftSwitchName = "2",
								RightSwitchName = "1"
			            	};
			Morphophonemic.PhonologicalRules.Add(rule1);

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("mui"), "51");
		}

		[Test]
		public void ComplexRule()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var rule1 = new MetathesisRule
			            	{
								Name = "rule1",
			            		Pattern = Pattern<Word, ShapeNode>.New()
			            			.Group("1", group => group.Annotation(Table3.GetSymbolFeatureStruct("i")))
									.Group("middle", group => group.Annotation(Table3.GetSymbolFeatureStruct("+")))
			            			.Group("2", group => group.Annotation(Table3.GetSymbolFeatureStruct("u")))
									.Group("rightEnv", group => group.Annotation(HCFeatureSystem.RightSideAnchor)).Value,
			            		LeftSwitchName = "2",
								RightSwitchName = "1"
			            	};
			Morphophonemic.PhonologicalRules.Add(rule1);

			var uSuffix = new AffixProcessRule
			              	{
								Name = "u_suffix",
			              		Gloss = "3SG"
			              	};
			Morphophonemic.MorphologicalRules.Add(uSuffix);
			uSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "+u")}
									});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("mui"), "53 3SG");
		}

		[Test]
		public void SimpleRuleNotUnapplied()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var prule = new MetathesisRule
			            	{
								Name = "rule1",
			            		Pattern = Pattern<Word, ShapeNode>.New()
			            			.Group("1", group => group.Annotation(Table3.GetSymbolFeatureStruct("i")))
			            			.Group("2", group => group.Annotation(Table3.GetSymbolFeatureStruct("u"))).Value,
			            		LeftSwitchName = "2",
								RightSwitchName = "1"
			            	};
			Morphophonemic.PhonologicalRules.Add(prule);

			var iSuffix = new AffixProcessRule
			              	{
								Name = "i_suffix",
			              		Gloss = "3SG"
			              	};
			Morphophonemic.MorphologicalRules.Add(iSuffix);
			iSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "i")}
									});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("pui"), "52 3SG");
		}
	}
}
