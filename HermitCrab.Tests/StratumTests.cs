using NUnit.Framework;
using SIL.HermitCrab.MorphologicalRules;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.Tests
{
	public class StratumTests : HermitCrabTestBase
	{
		[Test]
		public void ConsecutiveTemplates()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var alvStop = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident-")
				.Symbol("del_rel-")
				.Symbol("alveolar").Value;
			var voicelessCons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("vd-").Value;
			var voicelessLabiodental = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("labiodental")
				.Symbol("vd-").Value;
			var voiced = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("vd+").Value;
			var strident = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident+").Value;

			var edSuffix = new AffixProcessRule("ed_suffix")
			               	{
			               		Gloss = "PAST"
			               	};

			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(alvStop).Value
												},
											Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3, "ɯd") }
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo2")
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "t") }
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo3")
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "d") }
										});

			var verbTenseTemplate = new AffixTemplate("verbTense") { RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value };
			var vtslot1 = new AffixTemplateSlot("vtslot1") { Optional = true };
			vtslot1.Rules.Add(edSuffix);
			verbTenseTemplate.Slots.Add(vtslot1);
			Morphophonemic.AffixTemplates.Add(verbTenseTemplate);

			var sSuffix = new AffixProcessRule("s_suffix")
			              	{
			              		Gloss = "3SG"
			              	};

			sSuffix.Allomorphs.Add(new AffixProcessAllomorph("s_suffix_allo1")
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(voicelessLabiodental).Value
											},
										Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", voiced), new InsertShape(Table3, "z") }
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph("s_suffix_allo2")
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(strident).Value },
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "ɯz") }
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph("s_suffix_allo3")
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(voicelessCons).Value
											},
										Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3, "s") }
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph("s_suffix_allo2")
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "z") }
									});

			var verbPersonTemplate = new AffixTemplate("verbPerson") { RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value };
			var vpslot1 = new AffixTemplateSlot("vpslot1") { Optional = true };
			vpslot1.Rules.Add(sSuffix);
			verbPersonTemplate.Slots.Add(vpslot1);
			Morphophonemic.AffixTemplates.Add(verbPersonTemplate);

			var evidential = new AffixProcessRule("evidential")
								{
									Gloss = "WIT",
									RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								};

			evidential.Allomorphs.Add(new AffixProcessAllomorph("evidential_allo1")
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "v") }
										});
			Morphophonemic.MorphologicalRules.Add(evidential);

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sagd"), "32 ed_suffix");
			Assert.That(morpher.ParseWord("sagdz"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("sagdv"), "32 ed_suffix evidential");
			AssertMorphsEqual(morpher.ParseWord("sagdvɯz"), "32 ed_suffix evidential s_suffix");

		}
	}
}
