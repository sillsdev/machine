using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.Collections;
using SIL.HermitCrab.MorphologicalRules;
using SIL.HermitCrab.PhonologicalRules;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.Test
{
	public class AffixProcessRuleTest : HermitCrabTestBase
	{
		[Test]
		public void MorphosyntacticRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var sSuffix = new AffixProcessRule("s_suffix")
							{
								Gloss = "NMLZ",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value
							};
			Morphophonemic.MorphologicalRules.Add(sSuffix);

			sSuffix.Allomorphs.Add(new AffixProcessAllomorph("s_suffix_allo1")
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3.ToShape("s")) }
									});

			var morpher = new Morpher(SpanFactory, Language);
			List<Word> output = morpher.ParseWord("sags").ToList();
			AssertMorphsEqual(output, "32 s_suffix");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value);

			sSuffix.Gloss = "3.SG";
			sSuffix.OutSyntacticFeatureStruct = FeatureStruct.New().Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("sags").ToList();
			AssertMorphsEqual(output, "32 s_suffix");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value);

			sSuffix.Gloss = "NMLZ";
			sSuffix.RequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
			sSuffix.OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("sags").ToList();
			AssertMorphsEqual(output, "32 s_suffix");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value);

			sSuffix.Gloss = "3.SG";
			sSuffix.RequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
			sSuffix.OutSyntacticFeatureStruct = FeatureStruct.New().Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("sags").ToList();
			AssertMorphsEqual(output, "32 s_suffix");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value);

			sSuffix.Gloss = "NMLZ";
			sSuffix.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("tense").EqualTo("pres")).Value;
			sSuffix.OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("tense").EqualTo("past")).Value;
			sSuffix.Allomorphs.Clear();
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph("s_suffix_allo1")
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3.ToShape("d")) }
									});

			morpher = new Morpher(SpanFactory, Language);
			Assert.That(morpher.ParseWord("sid"), Is.Empty);
		}

		[Test]
		public void PercolationRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var rule1 = new AffixProcessRule("rule1")
							{
								Gloss = "3SG",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("2")).Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("2")).Value
							};
			Morphophonemic.MorphologicalRules.Add(rule1);

			rule1.Allomorphs.Add(new AffixProcessAllomorph("rule1_allo1")
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3.ToShape("z")) }
									});

			var morpher = new Morpher(SpanFactory, Language);
			List<Word> output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 rule1", "Perc3 rule1");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("2")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("2", "3")).Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 rule1", "Perc2 rule1", "Perc3 rule1", "Perc4 rule1");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("2")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("2")).Value;
			rule1.OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("3")).Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 rule1", "Perc3 rule1");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("3")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("2", "3")).Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 rule1", "Perc2 rule1", "Perc3 rule1", "Perc4 rule1");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("3")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("4")).Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 rule1");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("3")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("2")).Value;
			rule1.OutSyntacticFeatureStruct = FeatureStruct.New().Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 rule1", "Perc3 rule1");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("2")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("2", "3")).Value;

			morpher = new Morpher(SpanFactory, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 rule1", "Perc2 rule1", "Perc3 rule1", "Perc4 rule1");
			AssertSyntacticFeatureStructsEqual(output.Where(w => w.RootAllomorph.Morpheme.ID.IsOneOf("Perc0", "Perc3")), FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("2", "3")).Value);
			AssertSyntacticFeatureStructsEqual(output.Where(w => w.RootAllomorph.Morpheme.ID.IsOneOf("Perc2", "Perc4")), FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("3")).Value);
		}

		[Test]
		public void SuffixRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var strident = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident+").Value;
			var voicelessCons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("vd-").Value;
			var alvStop = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident-")
				.Symbol("del_rel-")
				.Symbol("alveolar")
				.Symbol("nasal-")
				.Symbol("asp-").Value;
			var d = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident-")
				.Symbol("del_rel-")
				.Symbol("alveolar")
				.Symbol("nasal-")
				.Symbol("vd+").Value;
			var unasp = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("asp-").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Value;

			var sSuffix = new AffixProcessRule("s_suffix")
			              	{
			              		Gloss = "3SG",
			              		RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
			              	};
			Morphophonemic.MorphologicalRules.Add(sSuffix);
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph("s_suffix_allo1")
			                       	{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(strident).Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3.ToShape("ɯz"))}
			                       	});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph("s_suffix_allo2")
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3.ToShape("s"))}
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph("s_suffix_allo3")
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3.ToShape("z"))}
									});

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
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(alvStop).Value
												},
											Rhs = {new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3.ToShape("+ɯd"))}
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo2")
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3.ToShape("+t"))}
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo3")
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3.ToShape("+")), new InsertShapeNode(d)}
										});

			var prule1 = new RewriteRule("rule1") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("t")).Value};
			Allophonic.PhonologicalRules.Add(prule1);
			prule1.Subrules.Add(new RewriteSubrule
									{
			                    		Rhs = Pattern<Word, ShapeNode>.New().Annotation(unasp).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(cons).Value
									});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("sagz"), "32 s_suffix");
			AssertMorphsEqual(morpher.ParseWord("sagd"), "32 ed_suffix");
			AssertMorphsEqual(morpher.ParseWord("sasɯz"), "33 s_suffix");
			AssertMorphsEqual(morpher.ParseWord("sast"), "33 ed_suffix");
			AssertMorphsEqual(morpher.ParseWord("sazd"), "34 ed_suffix");
			Assert.That(morpher.ParseWord("sagɯs"), Is.Empty);
			Assert.That(morpher.ParseWord("sags"), Is.Empty);
			Assert.That(morpher.ParseWord("sasz"), Is.Empty);
			Assert.That(morpher.ParseWord("sass"), Is.Empty);
			Assert.That(morpher.ParseWord("satɯs"), Is.Empty);
			Assert.That(morpher.ParseWord("satz"), Is.Empty);

			edSuffix.Allomorphs.RemoveAt(1);
			edSuffix.Allomorphs.RemoveAt(1);

			edSuffix.Allomorphs.Add(new AffixProcessAllomorph("ed_suffix_allo2")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1")
														.Annotation(any).OneOrMore
														.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, cons).Feature("vd").EqualToVariable("a").Value).Value 
												},
											Rhs = {new CopyFromInput("1"), new InsertShapeNode(FeatureStruct.New(Language.PhoneticFeatureSystem, alvStop).Feature("vd").EqualToVariable("a").Value)}
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("sagd"), "32 ed_suffix");
			AssertMorphsEqual(morpher.ParseWord("sast"), "33 ed_suffix");
			AssertMorphsEqual(morpher.ParseWord("sazd"), "34 ed_suffix");
		}

		[Test]
		public void PrefixRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var alvStop = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident-")
				.Symbol("cont-")
				.Symbol("del_rel-")
				.Symbol("alveolar")
				.Symbol("nasal-").Value;
			var voicelessCons = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("vd-").Value;
			var strident = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident+").Value;
			var voicelessStop = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("vd-")
				.Symbol("cont-").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Value;
			var lowVowel = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-")
				.Symbol("voc+")
				.Symbol("high-")
				.Symbol("low+")
				.Symbol("back+")
				.Symbol("round-")
				.Symbol("vd+")
				.Symbol("cont+").Value;
			var unasp = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("asp-").Value;

			var sPrefix = new AffixProcessRule("s_prefix")
							{
								Gloss = "3SG",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
							};
			Morphophonemic.MorphologicalRules.Add(sPrefix);
			sPrefix.Allomorphs.Add(new AffixProcessAllomorph("s_prefix_allo1")
			                		{
			                			Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(strident).Annotation(any).OneOrMore.Value},
										Rhs = {new InsertShape(Table3.ToShape("zi")), new CopyFromInput("1")}
			                		});
			sPrefix.Allomorphs.Add(new AffixProcessAllomorph("s_prefix_allo2")
			                       	{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(voicelessCons).Annotation(any).OneOrMore.Value},
										Rhs = {new InsertShape(Table3.ToShape("s")), new CopyFromInput("1")}
			                       	});
			sPrefix.Allomorphs.Add(new AffixProcessAllomorph("s_prefix_allo3")
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new InsertShape(Table3.ToShape("z")), new CopyFromInput("1")}
									});

			var edPrefix = new AffixProcessRule("ed_prefix")
							{
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("tense").EqualTo("past")).Value
							};
			Morphophonemic.MorphologicalRules.Add(edPrefix);
			edPrefix.Allomorphs.Add(new AffixProcessAllomorph("ed_prefix_allo1")
								{
									Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(alvStop).Annotation(any).OneOrMore.Value},
									Rhs = {new InsertShape(Table3.ToShape("di+")), new CopyFromInput("1")}
								});
			edPrefix.Allomorphs.Add(new AffixProcessAllomorph("ed_prefix_allo2")
								{
									Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(voicelessCons).Annotation(any).OneOrMore.Value},
									Rhs = {new InsertShape(Table3.ToShape("t+")), new CopyFromInput("1")}
								});
			edPrefix.Allomorphs.Add(new AffixProcessAllomorph("ed_prefix_allo3")
								{
									Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
									Rhs = {new InsertShape(Table3.ToShape("d+")), new CopyFromInput("1")}
								});

			var aspiration = new RewriteRule("aspiration") {Lhs = Pattern<Word, ShapeNode>.New().Annotation(voicelessStop).Value};
			Allophonic.PhonologicalRules.Add(aspiration);
			aspiration.Subrules.Add(new RewriteSubrule
										{
											Rhs = Pattern<Word, ShapeNode>.New().Annotation(unasp).Value,
										});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("zisag"), "s_prefix 32");
			AssertMorphsEqual(morpher.ParseWord("stag"), "s_prefix 47");
			AssertMorphsEqual(morpher.ParseWord("zabba"), "s_prefix 39", "s_prefix 40");
			AssertMorphsEqual(morpher.ParseWord("ditag"), "ed_prefix 47");
			AssertMorphsEqual(morpher.ParseWord("tpag"), "ed_prefix 48");
			AssertMorphsEqual(morpher.ParseWord("dabba"), "ed_prefix 39", "ed_prefix 40");
			Assert.That(morpher.ParseWord("zitag"), Is.Empty);
			Assert.That(morpher.ParseWord("sabba"), Is.Empty);
			Assert.That(morpher.ParseWord("ztag"), Is.Empty);
			Assert.That(morpher.ParseWord("disag"), Is.Empty);
			Assert.That(morpher.ParseWord("tabba"), Is.Empty);
			Assert.That(morpher.ParseWord("dtag"), Is.Empty);

			edPrefix.Allomorphs.RemoveAt(1);
			edPrefix.Allomorphs.RemoveAt(1);

			edPrefix.Allomorphs.Add(new AffixProcessAllomorph("ed_prefix_allo2")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1")
														.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem).Feature("vd").EqualToVariable("a").Value)
														.Annotation(any).OneOrMore.Value
												},
											Rhs = { new InsertShapeNode(FeatureStruct.New(Language.PhoneticFeatureSystem, alvStop).Feature("vd").EqualToVariable("a").Value), new CopyFromInput("1") }
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("ditag"), "ed_prefix 47");
			AssertMorphsEqual(morpher.ParseWord("tpag"), "ed_prefix 48");
			AssertMorphsEqual(morpher.ParseWord("dabba"), "ed_prefix 39", "ed_prefix 40");

			edPrefix.Allomorphs.Clear();

			edPrefix.Allomorphs.Add(new AffixProcessAllomorph("ed_prefix_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, lowVowel).Feature("ATR").EqualToVariable("a").Value).Value,
													Pattern<Word, ShapeNode>.New("3").Annotation(any).OneOrMore.Value
												},
											Rhs =
												{
													new InsertShape(Table3.ToShape("m")),
													new InsertShapeNode(FeatureStruct.New(Language.PhoneticFeatureSystem, lowVowel).Feature("ATR").EqualToVariable("a").Value),
													new CopyFromInput("1"),
													new CopyFromInput("3")
												}
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("masg"), "ed_prefix 32");
		}

		[Test]
		public void InfixRules()
		{
			var voicelessStop = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("vd-")
				.Symbol("cont-").Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Value;
			var unasp = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("asp-").Value;

			var perfAct = new AffixProcessRule("perf_act")
							{
								Gloss = "PER.ACT",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Symbol("V")
									.Feature("head").EqualTo(head => head
										.Feature("aspect").EqualTo("perf")
										.Feature("mood").EqualTo("active")).Value
							};
			Morphophonemic.MorphologicalRules.Add(perfAct);
			perfAct.Allomorphs.Add(new AffixProcessAllomorph("perf_act_allo1")
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(cons).Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(cons).Value,
												Pattern<Word, ShapeNode>.New("3").Annotation(cons).Value
											},
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3.ToShape("a")), new CopyFromInput("2"), new InsertShape(Table3.ToShape("a")), new CopyFromInput("3") }
									});

			var perfPass = new AffixProcessRule("perf_pass")
							{
								Gloss = "PER.PSV",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Symbol("V")
									.Feature("head").EqualTo(head => head
										.Feature("aspect").EqualTo("perf")
										.Feature("mood").EqualTo("passive")).Value
							};
			Morphophonemic.MorphologicalRules.Add(perfPass);
			perfPass.Allomorphs.Add(new AffixProcessAllomorph("perf_pass_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("3").Annotation(cons).Value
												},
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3.ToShape("u")), new CopyFromInput("2"), new InsertShape(Table3.ToShape("i")), new CopyFromInput("3") }
										});

			var imperfAct = new AffixProcessRule("imperf_act")
								{
									Gloss = "IMPF.ACT",
									RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
										.Symbol("V")
										.Feature("head").EqualTo(head => head
											.Feature("aspect").EqualTo("impf")
											.Feature("mood").EqualTo("active")).Value
								};
			Morphophonemic.MorphologicalRules.Add(imperfAct);
			imperfAct.Allomorphs.Add(new AffixProcessAllomorph("imperf_act_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(cons).Value
												},
											Rhs = { new InsertShape(Table3.ToShape("a")), new CopyFromInput("1"), new InsertShape(Table3.ToShape("u")), new CopyFromInput("2") }
										});

			var imperfPass = new AffixProcessRule("imperf_pass")
								{
									Gloss = "IMPF.PSV",
									RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
										.Symbol("V")
										.Feature("head").EqualTo(head => head
											.Feature("aspect").EqualTo("impf")
											.Feature("mood").EqualTo("passive")).Value
								};
			Morphophonemic.MorphologicalRules.Add(imperfPass);
			imperfPass.Allomorphs.Add(new AffixProcessAllomorph("imperf_pass_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("3").Annotation(cons).Value
												},
											Rhs = { new InsertShape(Table3.ToShape("u")), new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3.ToShape("a")), new CopyFromInput("3") }
										});

			var aspiration = new RewriteRule("aspiration") { Lhs = Pattern<Word, ShapeNode>.New().Annotation(voicelessStop).Value };
			Allophonic.PhonologicalRules.Add(aspiration);
			aspiration.Subrules.Add(new RewriteSubrule
										{
											Rhs = Pattern<Word, ShapeNode>.New().Annotation(unasp).Value,
										});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("katab"), "49 perf_act");
			AssertMorphsEqual(morpher.ParseWord("kutib"), "49 perf_pass");
			AssertMorphsEqual(morpher.ParseWord("aktub"), "imperf_act 49");
			AssertMorphsEqual(morpher.ParseWord("uktab"), "imperf_pass 49");
		}

		[Test]
		public void SimulfixRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Value;
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("voc+").Value;
			var voiced = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("vd+").Value;
			var nonround = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("round-").Value;
			var p = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("bilabial")
				.Symbol("nasal-")
				.Symbol("vd-").Value;

			var simulfix = new AffixProcessRule("simulfix")
							{
								Gloss = "SIMUL",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
							};
			Allophonic.MorphologicalRules.Add(simulfix);
			simulfix.Allomorphs.Add(new AffixProcessAllomorph("simulfix_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(p).Value
												},
											Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", voiced) }
										});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("pib"), "41 simulfix");

			simulfix.Allomorphs.Clear();
			simulfix.Allomorphs.Add(new AffixProcessAllomorph("simulfix_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(p).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value
												},
											Rhs = { new ModifyFromInput("1", voiced), new CopyFromInput("2") }
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bip"), "simulfix 41");

			simulfix.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value;
			simulfix.Allomorphs.Clear();
			simulfix.Allomorphs.Add(new AffixProcessAllomorph("simulfix_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Optional.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(vowel).Value,
													Pattern<Word, ShapeNode>.New("3").Annotation(any).OneOrMore.Value
												},
											Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", nonround), new CopyFromInput("3") }
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("bɯpu"), "46 simulfix");

			simulfix.Allomorphs.Clear();
			simulfix.Allomorphs.Add(new AffixProcessAllomorph("simulfix_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Optional.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(vowel).Range(1, 2).Value,
													Pattern<Word, ShapeNode>.New("3").Annotation(any).OneOrMore.Value
												},
											Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", nonround), new CopyFromInput("3") }
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("sɯɯpu"), "50 simulfix");
		}

		[Test]
		public void ReduplicationRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Value;
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("voc+").Value;
			var voiced = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("vd+").Value;
			var affricate = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("del_rel+")
				.Symbol("cont-").Value;

			var redup = new AffixProcessRule("redup")
							{
								Gloss = "RED",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
							};
			Morphophonemic.MorphologicalRules.Add(redup);
			redup.Allomorphs.Add(new AffixProcessAllomorph("redup_allo1")
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(cons).Annotation(vowel).Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value
											},
										Rhs = { new CopyFromInput("1"), new CopyFromInput("1"), new CopyFromInput("2") }
									});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("sasag"), "redup 32");

			var voicing = new RewriteRule("voicing") { Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("s")).Value };
			Allophonic.PhonologicalRules.Add(voicing);
			voicing.Subrules.Add(new RewriteSubrule
									{
										Rhs = Pattern<Word, ShapeNode>.New().Annotation(voiced).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value,
										RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
									});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("sazag"), "redup 32");

			var affrication = new RewriteRule("affrication") { Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("s")).Value };
			Allophonic.PhonologicalRules.Add(affrication);
			affrication.Subrules.Add(new RewriteSubrule
										{
											Rhs = Pattern<Word, ShapeNode>.New().Annotation(affricate).Value,
											LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.LeftSideAnchor).Value
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("tsazag"), "redup 32");

			redup.Allomorphs.Clear();
			redup.Allomorphs.Add(new AffixProcessAllomorph("redup_allo1")
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(vowel).Annotation(cons).Value,
											},
										Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new CopyFromInput("2") }
									});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("tsagag"), "32 redup");

			Allophonic.PhonologicalRules.Clear();

			redup.Allomorphs.Clear();
			redup.Allomorphs.Add(new AffixProcessAllomorph("redup_allo1")
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(any).ZeroOrMore.Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(cons).Annotation(vowel).Annotation(cons).Value,
											},
										Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new CopyFromInput("2") }
									});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("sagsag"), "32 redup");
			AssertMorphsEqual(morpher.ParseWord("sasibudbud"), "38 redup");

			redup.Allomorphs.Clear();
			redup.Allomorphs.Add(new AffixProcessAllomorph("redup_allo1")
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(vowel).Annotation(cons).Value,
											},
										Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new CopyFromInput("2") }
									});

			var gDelete = new RewriteRule("g_delete") { Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("g")).Value };
			Allophonic.PhonologicalRules.Add(gDelete);
			gDelete.Subrules.Add(new RewriteSubrule
									{
										LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value,
										RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
									});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("saag"), "32 redup");

			gDelete.Subrules.Clear();
			gDelete.Subrules.Add(new RewriteSubrule
									{
										RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.RightSideAnchor).Value
									});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("saga"), "32 redup");
		}

		[Test]
		public void TruncateRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var fricative = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Symbol("cont+").Value;
			var velarStop = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Symbol("velar").Value;

			var truncate = new AffixProcessRule("truncate")
							{
								Gloss = "3SG",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
							};
			Morphophonemic.MorphologicalRules.Add(truncate);
			truncate.Allomorphs.Add(new AffixProcessAllomorph("truncate_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(Table3.GetSymbolFeatureStruct("g")).Value
												},
											Rhs = { new CopyFromInput("1") }
										});

			var morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("sa"), "32");

			truncate.Allomorphs.Clear();
			truncate.Allomorphs.Add(new AffixProcessAllomorph("truncate_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(Table3.GetSymbolFeatureStruct("s")).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value
												},
											Rhs = { new CopyFromInput("2") }
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("ag"), "32");

			truncate.Allomorphs.Clear();
			truncate.Allomorphs.Add(new AffixProcessAllomorph("truncate_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(fricative).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value
												},
											Rhs = { new CopyFromInput("2") }
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("ag"), "32");

			truncate.Allomorphs.Clear();
			truncate.Allomorphs.Add(new AffixProcessAllomorph("truncate_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(velarStop).Value
												},
											Rhs = { new CopyFromInput("1") }
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("sa"), "32");

			truncate.Allomorphs.Clear();
			truncate.Allomorphs.Add(new AffixProcessAllomorph("truncate_allo1")
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(Table3.GetSymbolFeatureStruct("s")).Optional.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value
												},
											Rhs = { new InsertShape(Table3.ToShape("g")), new CopyFromInput("2") }
										});

			morpher = new Morpher(SpanFactory, Language);
			AssertMorphsEqual(morpher.ParseWord("gas"), "truncate 33");
			AssertMorphsEqual(morpher.ParseWord("gbubibi"), "truncate 42");
		}
	}
}
