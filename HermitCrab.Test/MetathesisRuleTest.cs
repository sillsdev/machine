using NUnit.Framework;
using SIL.HermitCrab.PhonologicalRules;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.Test
{
	public class MetathesisRuleTest : HermitCrabTestBase
	{
		[Test]
		public void SimpleRules()
		{
			var rule1 = new MetathesisRule("rule1")
			            	{
			            		Pattern = Pattern<Word, ShapeNode>.New()
			            			.Group("1", group => group.Annotation(Table1.GetSymbolFeatureStruct("u")))
			            			.Group("2", group => group.Annotation(Table1.GetSymbolFeatureStruct("p"))).Value,
			            		GroupOrder = {"2", "1"}
			            	};
			Allophonic.PhonologicalRules.Add(rule1);

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("supuu"), "50");


		}

		[Test]
		public void EnvironmentRules()
		{
			
		}
	}
}
