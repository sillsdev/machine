using System.Linq;
using NUnit.Framework;
using SIL.HermitCrab.MorphologicalRules;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.Tests
{
	public class AffixTemplateTests : HermitCrabTestBase
	{
		[Test]
		public void RealizationalRule()
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
			var labiodental = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("labiodental").Value;
			var voiced = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("vd+").Value;
			var strident = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident+").Value;

			var edSuffix = new RealizationalAffixProcessRule
			               	{
								Name = "ed_suffix",
								RealizationalFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("tense").EqualTo("past")).Value,
			               		Gloss = "PAST"
			               	};

			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(alvStop).Value
												},
											Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3, "ɯd") }
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "t") }
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "d") }
										});

			var sSuffix = new RealizationalAffixProcessRule
			              	{
								Name = "s_suffix",
								RealizationalFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")
										.Feature("tense").EqualTo("pres")).Value,
			              		Gloss = "3SG"
			              	};

			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(labiodental).Value
											},
										Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", voiced), new InsertShape(Table3, "z") }
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(strident).Value },
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "ɯz") }
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(voicelessCons).Value
											},
										Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3, "s") }
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "z") }
									});

			var evidential = new RealizationalAffixProcessRule
								{
									Name = "evidential",
									RealizationalFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
										.Feature("head").EqualTo(head => head
											.Feature("evidential").EqualTo("witnessed")).Value,
									Gloss = "WIT"
								};

			evidential.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "v") }
										});

			var verbTemplate = new AffixTemplate { Name = "verb", RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value };
			var slot1 = new AffixTemplateSlot {Optional = true};
			slot1.Rules.Add(sSuffix);
			slot1.Rules.Add(edSuffix);
			verbTemplate.Slots.Add(slot1);
			var slot2 = new AffixTemplateSlot {Optional = true};
			slot2.Rules.Add(evidential);
			verbTemplate.Slots.Add(slot2);
			Morphophonemic.AffixTemplates.Add(verbTemplate);

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			Word[] output = morpher.ParseWord("sagd").ToArray();
			AssertMorphsEqual(output, "32 PAST");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("tense").EqualTo("past")).Value);
			output = morpher.ParseWord("sagdv").ToArray();
			AssertMorphsEqual(output, "32 PAST WIT");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("tense").EqualTo("past")
					.Feature("evidential").EqualTo("witnessed")).Value);
			Assert.That(morpher.ParseWord("sid"), Is.Empty);
			output = morpher.ParseWord("sau").ToArray();
			AssertMorphsEqual(output, "bl2");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("tense").EqualTo("past")).Value);

			evidential.RealizationalFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("evidential").EqualTo("witnessed")
					.Feature("tense").EqualTo("pres")).Value;

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			output = morpher.ParseWord("sagzv").ToArray();
			AssertMorphsEqual(output, "32 3SG WIT");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("3")
					.Feature("tense").EqualTo("pres")
					.Feature("evidential").EqualTo("witnessed")).Value);
		}

		[Test]
		public void NonFinalTemplate()
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

			var edSuffix = new AffixProcessRule
			               	{
								Name = "ed_suffix",
			               		Gloss = "PAST"
			               	};

			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(alvStop).Value
												},
											Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3, "ɯd") }
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "t") }
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "d") }
										});

			var verbTemplate = new AffixTemplate { Name = "verb", RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value };
			var slot1 = new AffixTemplateSlot();
			slot1.Rules.Add(edSuffix);
			verbTemplate.Slots.Add(slot1);
			Morphophonemic.AffixTemplates.Add(verbTemplate);

			var nominalizer = new AffixProcessRule
								{
									Name = "nominalizer",
									Gloss = "NOM",
									RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
									OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value
								};

			nominalizer.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "v") }
										});
			Morphophonemic.MorphologicalRules.Add(nominalizer);

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sagd"), "32 PAST");
			AssertMorphsEqual(morpher.ParseWord("sagdv"), "32 PAST NOM");

			verbTemplate.IsFinal = false;
			morpher = new Morpher(SpanFactory, TraceManager, Language);
			Assert.That(morpher.ParseWord("sagd"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("sagdv"), "32 PAST NOM");
		}

		[Test]
		public void SameRuleUsedInMultipleTemplates()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var edSuffix = new AffixProcessRule
			               	{
								Name = "ed_suffix",
			               		Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V", "IV", "TV").Value
			               	};

			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "d") }
										});

			var transitiveVerbTemplate = new AffixTemplate { Name = "Transitive Verb", RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("TV").Value };
			var slot1 = new AffixTemplateSlot();
			slot1.Rules.Add(edSuffix);
			transitiveVerbTemplate.Slots.Add(slot1);
			Morphophonemic.AffixTemplates.Add(transitiveVerbTemplate);

			var intransitiveVerbTemplate = new AffixTemplate { Name = "Intransitive Verb", RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("IV").Value };
			slot1 = new AffixTemplateSlot();
			slot1.Rules.Add(edSuffix);
			intransitiveVerbTemplate.Slots.Add(slot1);
			Morphophonemic.AffixTemplates.Add(intransitiveVerbTemplate);

			var nominalizer = new AffixProcessRule
								{
									Name = "intransitive verbalizer",
									Gloss = "IVERB",
									RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
									OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("IV").Value
								};

			nominalizer.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "v") }
										});
			Morphophonemic.MorphologicalRules.Add(nominalizer);

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("mivd"), "53 IVERB PAST");
		}
	}
}
