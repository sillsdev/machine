using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.HermitCrab;
using SIL.HermitCrab.MorphologicalRules;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace HermitCrabTest
{
	public class CompoundingRuleTest : HermitCrabTestBase
	{
		[Test]
		public void SimpleRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var rule1 = new CompoundingRule("rule1");
			Allophonic.MorphologicalRules.Add(rule1);
			rule1.Subrules.Add(new CompoundingSubrule("rule1_sr1")
			                   	{
			                   		HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
									NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
									Rhs = { new CopyFromInput("head"), new InsertShape(Table3.ToShape("+")), new CopyFromInput("nonHead") }
			                   	});

			var morpher = new Morpher(SpanFactory, Language);
			List<Word> output = morpher.ParseWord("pʰutdat").ToList();
			AssertMorphsEqual(output, "5 8", "5 9");
			AssertRootAllomorphsEquals(output, "5");
			Assert.IsFalse(morpher.ParseWord("pʰutdas").Any());
			Assert.IsFalse(morpher.ParseWord("pʰusdat").Any());

			rule1.Subrules.Clear();
			rule1.Subrules.Add(new CompoundingSubrule("rule1_sr1")
								{
									HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
									NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
									Rhs = { new CopyFromInput("nonHead"), new InsertShape(Table3.ToShape("+")), new CopyFromInput("head") }
								});

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("pʰutdat").ToList();
			AssertMorphsEqual(output, "5 8", "5 9");
			AssertRootAllomorphsEquals(output, "8", "9");
			Assert.IsFalse(morpher.ParseWord("pʰutdas").Any());
			Assert.IsFalse(morpher.ParseWord("pʰusdat").Any());

			var prefix = new AffixProcessRule("prefix")
			{
				Gloss = "PAST",
				RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
				OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
					.Feature("head").EqualToFeatureStruct(head => head
						.Feature("tense").EqualTo("past")).Value
			};
			Allophonic.MorphologicalRules.Insert(0, prefix);
			prefix.Allomorphs.Add(new AffixProcessAllomorph("prefix_allo1")
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
										Rhs = { new InsertShape(Table3.ToShape("di+")), new CopyFromInput("1") }
									});

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("pʰutdidat").ToList();
			AssertMorphsEqual(output, "5 prefix 9");
			AssertRootAllomorphsEquals(output, "9");

			Allophonic.MorphologicalRules.RemoveAt(0);

			rule1.MaxApplicationCount = 2;
			rule1.Subrules.Clear();
			rule1.Subrules.Add(new CompoundingSubrule("rule1_sr1")
								{
									HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
									NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
									Rhs = { new CopyFromInput("head"), new InsertShape(Table3.ToShape("+")), new CopyFromInput("nonHead") }
								});

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("pʰutdatpip").ToList();
			AssertMorphsEqual(output, "5 8 41", "5 9 41");
			AssertRootAllomorphsEquals(output, "5");

			rule1.MaxApplicationCount = 1;

			var rule2 = new CompoundingRule("rule2");
			Allophonic.MorphologicalRules.Add(rule2);
			rule2.Subrules.Add(new CompoundingSubrule("rule2_sr1")
								{
									HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
									NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
									Rhs = { new CopyFromInput("nonHead"), new InsertShape(Table3.ToShape("+")), new CopyFromInput("head") },
								});

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("pʰutdatpip").ToList();
			AssertMorphsEqual(output, "5 8 41", "5 9 41");
			AssertRootAllomorphsEquals(output, "8", "9");
		}

		[Test]
		public void MorphosyntacticRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var rule1 = new CompoundingRule("rule1") { NonHeadRequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value };
			Allophonic.MorphologicalRules.Add(rule1);
			rule1.Subrules.Add(new CompoundingSubrule("rule1_sr1")
								{
									HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
									NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
									Rhs = { new CopyFromInput("head"), new InsertShape(Table3.ToShape("+")), new CopyFromInput("nonHead") }
								});

			var morpher = new Morpher(SpanFactory, Language);
			List<Word> output = morpher.ParseWord("pʰutdat").ToList();
			AssertMorphsEqual(output, "5 9");
			AssertRootAllomorphsEquals(output, "5");
			Assert.AreEqual(FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value, output[0].SyntacticFeatureStruct);
			Assert.IsFalse(morpher.ParseWord("pʰutbupu").Any());

			rule1.OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("pʰutdat").ToList();
			AssertMorphsEqual(output, "5 9");
			AssertRootAllomorphsEquals(output, "5");
			Assert.AreEqual(FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value, output[0].SyntacticFeatureStruct);

			Allophonic.MorphologicalRules.Clear();
			Morphophonemic.MorphologicalRules.Add(rule1);
			rule1.HeadRequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("pers").EqualTo("2")).Value;
			rule1.NonHeadRequiredSyntacticFeatureStruct = FeatureStruct.New().Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("ssagabba").ToList();
			AssertMorphsEqual(output, "Perc0 39", "Perc0 40", "Perc3 39", "Perc3 40");
			AssertRootAllomorphsEquals(output, "Perc0", "Perc3");
		}

		private void AssertRootAllomorphsEquals(IEnumerable<Word> words, params string[] expected)
		{
			var actual = new HashSet<string>(words.Select(w => w.RootAllomorph.Morpheme.ID));
			Assert.AreEqual(expected.Length, actual.Count);
			foreach (string id in expected)
				Assert.IsTrue(actual.Contains(id));
		}
	}
}
