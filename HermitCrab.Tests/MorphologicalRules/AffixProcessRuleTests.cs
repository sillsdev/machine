using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.Collections;
using SIL.HermitCrab.MorphologicalRules;
using SIL.HermitCrab.PhonologicalRules;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.Tests.MorphologicalRules
{
	public class AffixProcessRuleTests : HermitCrabTestBase
	{
		[Test]
		public void MorphosyntacticRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var sSuffix = new AffixProcessRule
							{
								Name = "s_suffix",
								Gloss = "NMLZ",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value
							};
			Morphophonemic.MorphologicalRules.Add(sSuffix);

			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "s") }
									});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			List<Word> output = morpher.ParseWord("sags").ToList();
			AssertMorphsEqual(output, "32 NMLZ");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value);

			sSuffix.Gloss = "3.SG";
			sSuffix.OutSyntacticFeatureStruct = FeatureStruct.New().Value;

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			output = morpher.ParseWord("sags").ToList();
			AssertMorphsEqual(output, "32 3.SG");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value);

			sSuffix.Gloss = "NMLZ";
			sSuffix.RequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
			sSuffix.OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value;

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			output = morpher.ParseWord("sags").ToList();
			AssertMorphsEqual(output, "32 NMLZ");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value);

			sSuffix.Gloss = "3.SG";
			sSuffix.RequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
			sSuffix.OutSyntacticFeatureStruct = FeatureStruct.New().Value;

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			output = morpher.ParseWord("sags").ToList();
			AssertMorphsEqual(output, "32 3.SG");
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
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "d") }
									});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			Assert.That(morpher.ParseWord("sid"), Is.Empty);
		}

		[Test]
		public void PercolationRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var rule1 = new AffixProcessRule
							{
								Name = "rule1",
								Gloss = "3SG",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("2")).Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("2")).Value
							};
			Morphophonemic.MorphologicalRules.Add(rule1);

			rule1.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "z") }
									});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			List<Word> output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 3SG", "Perc3 3SG");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("2")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("2", "3")).Value;

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 3SG", "Perc2 3SG", "Perc3 3SG", "Perc4 3SG");
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

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 3SG", "Perc3 3SG");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("3")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("2", "3")).Value;

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 3SG", "Perc2 3SG", "Perc3 3SG", "Perc4 3SG");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("3")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("4")).Value;

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 3SG");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("3")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("2")).Value;
			rule1.OutSyntacticFeatureStruct = FeatureStruct.New().Value;

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 3SG", "Perc3 3SG");
			AssertSyntacticFeatureStructsEqual(output, FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("2")).Value);

			rule1.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("2", "3")).Value;

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			output = morpher.ParseWord("ssagz").ToList();
			AssertMorphsEqual(output, "Perc0 3SG", "Perc2 3SG", "Perc3 3SG", "Perc4 3SG");
			AssertSyntacticFeatureStructsEqual(output.Where(w => w.RootAllomorph.Morpheme.ToString().IsOneOf("Perc0", "Perc3")), FeatureStruct.New(Language.SyntacticFeatureSystem)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")
					.Feature("pers").EqualTo("2", "3")).Value);
			AssertSyntacticFeatureStructsEqual(output.Where(w => w.RootAllomorph.Morpheme.ToString().IsOneOf("Perc2", "Perc4")), FeatureStruct.New(Language.SyntacticFeatureSystem)
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

			var sSuffix = new AffixProcessRule
			              	{
								Name = "s_suffix",
			              		Gloss = "3SG",
			              		RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
			              	};
			Morphophonemic.MorphologicalRules.Add(sSuffix);
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
			                       	{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(strident).Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "ɯz")}
			                       	});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "s")}
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "z")}
									});

			var edSuffix = new AffixProcessRule
							{
								Name = "ed_suffix",
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("tense").EqualTo("past")).Value
							};
			Morphophonemic.MorphologicalRules.Add(edSuffix);
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(alvStop).Value
												},
											Rhs = {new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3, "+ɯd")}
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+t")}
										});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+"), new InsertShapeNode(d)}
										});

			var prule1 = new RewriteRule { Name = "rule1", Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table3.GetSymbolFeatureStruct("t")).Value };
			Allophonic.PhonologicalRules.Add(prule1);
			prule1.Subrules.Add(new RewriteSubrule
									{
			                    		Rhs = Pattern<Word, ShapeNode>.New().Annotation(unasp).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(cons).Value
									});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sagz"), "32 3SG");
			AssertMorphsEqual(morpher.ParseWord("sagd"), "32 PAST");
			AssertMorphsEqual(morpher.ParseWord("sasɯz"), "33 3SG");
			AssertMorphsEqual(morpher.ParseWord("sast"), "33 PAST");
			AssertMorphsEqual(morpher.ParseWord("sazd"), "34 PAST");
			Assert.That(morpher.ParseWord("sagɯs"), Is.Empty);
			Assert.That(morpher.ParseWord("sags"), Is.Empty);
			Assert.That(morpher.ParseWord("sasz"), Is.Empty);
			Assert.That(morpher.ParseWord("sass"), Is.Empty);
			Assert.That(morpher.ParseWord("satɯs"), Is.Empty);
			Assert.That(morpher.ParseWord("satz"), Is.Empty);

			edSuffix.Allomorphs.RemoveAt(1);
			edSuffix.Allomorphs.RemoveAt(1);

			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1")
														.Annotation(any).OneOrMore
														.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, cons).Feature("vd").EqualToVariable("a").Value).Value 
												},
											Rhs = {new CopyFromInput("1"), new InsertShapeNode(FeatureStruct.New(Language.PhoneticFeatureSystem, alvStop).Feature("vd").EqualToVariable("a").Value)}
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sagd"), "32 PAST");
			AssertMorphsEqual(morpher.ParseWord("sast"), "33 PAST");
			AssertMorphsEqual(morpher.ParseWord("sazd"), "34 PAST");
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

			var sPrefix = new AffixProcessRule
							{
								Name = "s_prefix",
								Gloss = "3SG",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
							};
			Morphophonemic.MorphologicalRules.Add(sPrefix);
			sPrefix.Allomorphs.Add(new AffixProcessAllomorph
			                		{
			                			Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(strident).Annotation(any).OneOrMore.Value},
										Rhs = {new InsertShape(Table3, "zi"), new CopyFromInput("1")}
			                		});
			sPrefix.Allomorphs.Add(new AffixProcessAllomorph
			                       	{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(voicelessCons).Annotation(any).OneOrMore.Value},
										Rhs = {new InsertShape(Table3, "s"), new CopyFromInput("1")}
			                       	});
			sPrefix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new InsertShape(Table3, "z"), new CopyFromInput("1")}
									});

			var edPrefix = new AffixProcessRule
							{
								Name = "ed_prefix",
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("tense").EqualTo("past")).Value
							};
			Morphophonemic.MorphologicalRules.Add(edPrefix);
			edPrefix.Allomorphs.Add(new AffixProcessAllomorph
								{
									Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(alvStop).Annotation(any).OneOrMore.Value},
									Rhs = {new InsertShape(Table3, "di+"), new CopyFromInput("1")}
								});
			edPrefix.Allomorphs.Add(new AffixProcessAllomorph
								{
									Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(voicelessCons).Annotation(any).OneOrMore.Value},
									Rhs = {new InsertShape(Table3, "t+"), new CopyFromInput("1")}
								});
			edPrefix.Allomorphs.Add(new AffixProcessAllomorph
								{
									Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
									Rhs = {new InsertShape(Table3, "d+"), new CopyFromInput("1")}
								});

			var aspiration = new RewriteRule { Name = "aspiration", Lhs = Pattern<Word, ShapeNode>.New().Annotation(voicelessStop).Value };
			Allophonic.PhonologicalRules.Add(aspiration);
			aspiration.Subrules.Add(new RewriteSubrule
										{
											Rhs = Pattern<Word, ShapeNode>.New().Annotation(unasp).Value,
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("zisag"), "3SG 32");
			AssertMorphsEqual(morpher.ParseWord("stag"), "3SG 47");
			AssertMorphsEqual(morpher.ParseWord("zabba"), "3SG 39", "3SG 40");
			AssertMorphsEqual(morpher.ParseWord("ditag"), "PAST 47");
			AssertMorphsEqual(morpher.ParseWord("tpag"), "PAST 48");
			AssertMorphsEqual(morpher.ParseWord("dabba"), "PAST 39", "PAST 40");
			Assert.That(morpher.ParseWord("zitag"), Is.Empty);
			Assert.That(morpher.ParseWord("sabba"), Is.Empty);
			Assert.That(morpher.ParseWord("ztag"), Is.Empty);
			Assert.That(morpher.ParseWord("disag"), Is.Empty);
			Assert.That(morpher.ParseWord("tabba"), Is.Empty);
			Assert.That(morpher.ParseWord("dtag"), Is.Empty);

			edPrefix.Allomorphs.RemoveAt(1);
			edPrefix.Allomorphs.RemoveAt(1);

			edPrefix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1")
														.Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment).Feature("vd").EqualToVariable("a").Value)
														.Annotation(any).OneOrMore.Value
												},
											Rhs = { new InsertShapeNode(FeatureStruct.New(Language.PhoneticFeatureSystem, alvStop).Feature("vd").EqualToVariable("a").Value), new CopyFromInput("1") }
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("ditag"), "PAST 47");
			AssertMorphsEqual(morpher.ParseWord("tpag"), "PAST 48");
			AssertMorphsEqual(morpher.ParseWord("dabba"), "PAST 39", "PAST 40");

			edPrefix.Allomorphs.Clear();

			edPrefix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(FeatureStruct.New(Language.PhoneticFeatureSystem, lowVowel).Feature("ATR").EqualToVariable("a").Value).Value,
													Pattern<Word, ShapeNode>.New("3").Annotation(any).OneOrMore.Value
												},
											Rhs =
												{
													new InsertShape(Table3, "m"),
													new InsertShapeNode(FeatureStruct.New(Language.PhoneticFeatureSystem, lowVowel).Feature("ATR").EqualToVariable("a").Value),
													new CopyFromInput("1"),
													new CopyFromInput("3")
												}
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("masg"), "PAST 32");
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

			var perfAct = new AffixProcessRule
							{
								Name = "perf_act",
								Gloss = "PER.ACT",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Symbol("V")
									.Feature("head").EqualTo(head => head
										.Feature("aspect").EqualTo("perf")
										.Feature("mood").EqualTo("active")).Value
							};
			Morphophonemic.MorphologicalRules.Add(perfAct);
			perfAct.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(cons).Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(cons).Value,
												Pattern<Word, ShapeNode>.New("3").Annotation(cons).Value
											},
										Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "a"), new CopyFromInput("2"), new InsertShape(Table3, "a"), new CopyFromInput("3") }
									});

			var perfPass = new AffixProcessRule
							{
								Name = "pref_pass",
								Gloss = "PER.PSV",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Symbol("V")
									.Feature("head").EqualTo(head => head
										.Feature("aspect").EqualTo("perf")
										.Feature("mood").EqualTo("passive")).Value
							};
			Morphophonemic.MorphologicalRules.Add(perfPass);
			perfPass.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("3").Annotation(cons).Value
												},
											Rhs = { new CopyFromInput("1"), new InsertShape(Table3, "u"), new CopyFromInput("2"), new InsertShape(Table3, "i"), new CopyFromInput("3") }
										});

			var imperfAct = new AffixProcessRule
								{
									Name = "imperf_act",
									Gloss = "IMPF.ACT",
									RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
										.Symbol("V")
										.Feature("head").EqualTo(head => head
											.Feature("aspect").EqualTo("impf")
											.Feature("mood").EqualTo("active")).Value
								};
			Morphophonemic.MorphologicalRules.Add(imperfAct);
			imperfAct.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(cons).Value
												},
											Rhs = { new InsertShape(Table3, "a"), new CopyFromInput("1"), new InsertShape(Table3, "u"), new CopyFromInput("2") }
										});

			var imperfPass = new AffixProcessRule
								{
									Name = "imperf_pass",
									Gloss = "IMPF.PSV",
									RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
										.Symbol("V")
										.Feature("head").EqualTo(head => head
											.Feature("aspect").EqualTo("impf")
											.Feature("mood").EqualTo("passive")).Value
								};
			Morphophonemic.MorphologicalRules.Add(imperfPass);
			imperfPass.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(cons).Value,
													Pattern<Word, ShapeNode>.New("3").Annotation(cons).Value
												},
											Rhs = { new InsertShape(Table3, "u"), new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3, "a"), new CopyFromInput("3") }
										});

			var aspiration = new RewriteRule { Name = "aspiration", Lhs = Pattern<Word, ShapeNode>.New().Annotation(voicelessStop).Value };
			Allophonic.PhonologicalRules.Add(aspiration);
			aspiration.Subrules.Add(new RewriteSubrule
										{
											Rhs = Pattern<Word, ShapeNode>.New().Annotation(unasp).Value,
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("katab"), "49 PER.ACT");
			AssertMorphsEqual(morpher.ParseWord("kutib"), "49 PER.PSV");
			AssertMorphsEqual(morpher.ParseWord("aktub"), "IMPF.ACT 49");
			AssertMorphsEqual(morpher.ParseWord("uktab"), "IMPF.PSV 49");
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

			var simulfix = new AffixProcessRule
							{
								Name = "simulfix",
								Gloss = "SIMUL",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
							};
			Allophonic.MorphologicalRules.Add(simulfix);
			simulfix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(p).Value
												},
											Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", voiced) }
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("pib"), "41 SIMUL");

			simulfix.Allomorphs.Clear();
			simulfix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(p).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value
												},
											Rhs = { new ModifyFromInput("1", voiced), new CopyFromInput("2") }
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("bip"), "SIMUL 41");

			simulfix.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value;
			simulfix.Allomorphs.Clear();
			simulfix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Optional.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(vowel).Value,
													Pattern<Word, ShapeNode>.New("3").Annotation(any).OneOrMore.Value
												},
											Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", nonround), new CopyFromInput("3") }
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("bɯpu"), "46 SIMUL");

			simulfix.Allomorphs.Clear();
			simulfix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(cons).Optional.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(vowel).Range(1, 2).Value,
													Pattern<Word, ShapeNode>.New("3").Annotation(any).OneOrMore.Value
												},
											Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", nonround), new CopyFromInput("3") }
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sɯɯpu"), "50 SIMUL");
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

			var redup = new AffixProcessRule
							{
								Name = "redup",
								Gloss = "RED",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
							};
			Morphophonemic.MorphologicalRules.Add(redup);
			redup.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(cons).Annotation(vowel).Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value
											},
										Rhs = { new CopyFromInput("1"), new CopyFromInput("1"), new CopyFromInput("2") }
									});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sasag"), "RED 32");

			var voicing = new RewriteRule { Name = "voicing", Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("s")).Value };
			Allophonic.PhonologicalRules.Add(voicing);
			voicing.Subrules.Add(new RewriteSubrule
									{
										Rhs = Pattern<Word, ShapeNode>.New().Annotation(voiced).Value,
										LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value,
										RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
									});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sazag"), "RED 32");

			var affrication = new RewriteRule { Name = "affrication", Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("s")).Value };
			Allophonic.PhonologicalRules.Add(affrication);
			affrication.Subrules.Add(new RewriteSubrule
										{
											Rhs = Pattern<Word, ShapeNode>.New().Annotation(affricate).Value,
											LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.LeftSideAnchor).Value
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("tsazag"), "RED 32");

			redup.Allomorphs.Clear();
			redup.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(vowel).Annotation(cons).Value,
											},
										Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new CopyFromInput("2") }
									});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("tsagag"), "32 RED");

			Allophonic.PhonologicalRules.Clear();

			redup.Allomorphs.Clear();
			redup.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(any).ZeroOrMore.Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(cons).Annotation(vowel).Annotation(cons).Value,
											},
										Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new CopyFromInput("2") }
									});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sagsag"), "32 RED");
			AssertMorphsEqual(morpher.ParseWord("sasibudbud"), "38 RED");

			redup.Allomorphs.Clear();
			redup.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs =
											{
												Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
												Pattern<Word, ShapeNode>.New("2").Annotation(vowel).Annotation(cons).Value,
											},
										Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new CopyFromInput("2") }
									});

			var gDelete = new RewriteRule { Name = "g_delete", Lhs = Pattern<Word, ShapeNode>.New().Annotation(Table1.GetSymbolFeatureStruct("g")).Value };
			Allophonic.PhonologicalRules.Add(gDelete);
			gDelete.Subrules.Add(new RewriteSubrule
									{
										LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value,
										RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
									});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("saag"), "32 RED");

			gDelete.Subrules.Clear();
			gDelete.Subrules.Add(new RewriteSubrule
									{
										RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(HCFeatureSystem.RightSideAnchor).Value
									});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("saga"), "32 RED");
		}

		[Test]
		public void TruncateRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var fricative = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Symbol("cont+").Value;
			var velarStop = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Symbol("velar").Value;

			var truncate = new AffixProcessRule
							{
								Name = "truncate",
								Gloss = "3SG",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
							};
			Morphophonemic.MorphologicalRules.Add(truncate);
			truncate.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(Table3.GetSymbolFeatureStruct("g")).Value
												},
											Rhs = { new CopyFromInput("1") }
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sa"), "32");

			truncate.Allomorphs.Clear();
			truncate.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(Table3.GetSymbolFeatureStruct("s")).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value
												},
											Rhs = { new CopyFromInput("2") }
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("ag"), "32");

			truncate.Allomorphs.Clear();
			truncate.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(fricative).Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value
												},
											Rhs = { new CopyFromInput("2") }
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("ag"), "32");

			truncate.Allomorphs.Clear();
			truncate.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(velarStop).Value
												},
											Rhs = { new CopyFromInput("1") }
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sa"), "32");

			truncate.Allomorphs.Clear();
			truncate.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs =
												{
													Pattern<Word, ShapeNode>.New("1").Annotation(Table3.GetSymbolFeatureStruct("s")).Optional.Value,
													Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value
												},
											Rhs = { new InsertShape(Table3, "g"), new CopyFromInput("2") }
										});

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("gas"), "3SG 33");
			AssertMorphsEqual(morpher.ParseWord("gbubibi"), "3SG 42");
		}

		[Test]
		public void RequiredEnvironments()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("voc+").Value;

			var sSuffix = new AffixProcessRule
			              	{
								Name = "s_suffix",
			              		Gloss = "3SG",
			              		RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
			              	};
			Morphophonemic.MorphologicalRules.Add(sSuffix);
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "s")},
										RequiredEnvironments = {new AllomorphEnvironment(SpanFactory, null, Pattern<Word, ShapeNode>.New().Annotation(vowel).Value)}
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "z")}
									});

			var edSuffix = new AffixProcessRule
							{
								Name = "ed_suffix",
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("tense").EqualTo("past")).Value
							};
			Morphophonemic.MorphologicalRules.Add(edSuffix);
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+ɯd")}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sagz"), "32 3SG");
			Assert.That(morpher.ParseWord("sags"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("sagsɯd"), "32 3SG PAST");
			Assert.That(morpher.ParseWord("sagzɯd"), Is.Empty);
		}

		[Test]
		public void RequiredSyntacticFeatureStruct()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var sSuffix = new AffixProcessRule
			              	{
								Name = "s_suffix",
			              		Gloss = "3SG",
			              		RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
			              	};
			Morphophonemic.MorphologicalRules.Add(sSuffix);
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "s")},
										RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Feature("head").EqualTo(head => head.Feature("tense").EqualTo("past")).Value
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "z")}
									});

			var edSuffix = new AffixProcessRule
							{
								Name = "ed_suffix",
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("tense").EqualTo("past")).Value
							};
			Morphophonemic.MorphologicalRules.Add(edSuffix);
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+ɯd")}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("bagz"), "synfs 3SG");
			Assert.That(morpher.ParseWord("bags"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("bagsɯd"), "synfs 3SG PAST");
			Assert.That(morpher.ParseWord("bagzɯd"), Is.Empty);
		}

		[Test]
		public void FreeFluctuation()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var sSuffix = new AffixProcessRule
			              	{
								Name = "s_suffix",
			              		Gloss = "3SG",
			              		RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
			              	};
			Morphophonemic.MorphologicalRules.Add(sSuffix);
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "s")}
									});
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "z")}
									});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sagz"), "32 3SG");
			AssertMorphsEqual(morpher.ParseWord("sags"), "32 3SG");
			AssertMorphsEqual(morpher.ParseWord("tass"), "free 3SG");
			AssertMorphsEqual(morpher.ParseWord("tazs"), "free 3SG");
			AssertMorphsEqual(morpher.ParseWord("tasz"), "free 3SG");
			AssertMorphsEqual(morpher.ParseWord("tazz"), "free 3SG");
		}

		[Test]
		public void CircumfixRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var circumfix = new AffixProcessRule
							{
								Name = "circumfix",
								Gloss = "OBJ"
							};
			Morphophonemic.MorphologicalRules.Add(circumfix);
			circumfix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new InsertShape(Table3, "ta"), new CopyFromInput("1"), new InsertShape(Table3, "od")}
									});

			var sSuffix = new AffixProcessRule
			              	{
								Name = "s_suffix",
			              		Gloss = "3SG"
			              	};
			Morphophonemic.MorphologicalRules.Add(sSuffix);
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
										Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "s")}
									});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("tasagods"), "OBJ 32 3SG");
		}

		[Test]
		public void BoundaryRules()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var cons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+").Value;
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons-").Value;

			var sSuffix = new AffixProcessRule
			              	{
								Name = "s_suffix",
			              		Gloss = "3SG",
			              		RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("pers").EqualTo("3")).Value
			              	};
			Morphophonemic.MorphologicalRules.Add(sSuffix);
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
									{
										Lhs =
										{
											Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
											Pattern<Word, ShapeNode>.New("2")
												.Annotation(FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Feature(HCFeatureSystem.StrRep).EqualTo("+").Value)
												.Annotation(cons)
												.Annotation(vowel).Value
										},
										Rhs = {new CopyFromInput("1"), new CopyFromInput("2"), new InsertShape(Table3, "s")}
									});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("abbas"), "39 3SG");
		}
	}
}
