using System.Linq;
using NUnit.Framework;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.HermitCrab.MorphologicalRules;
using SIL.Machine.Matching;

namespace SIL.Machine.HermitCrab.Tests
{
	public class LexEntryTests : HermitCrabTestBase
	{
		[Test]
		public void DisjunctiveAllomorphs()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var edSuffix = new AffixProcessRule
							{
								Name = "ed_suffix",
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
							};
			Morphophonemic.MorphologicalRules.Add(edSuffix);
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+ɯd")}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("bazɯd"), "disj PAST");
			Assert.That(morpher.ParseWord("batɯd"), Is.Empty);
			Assert.That(morpher.ParseWord("badɯd"), Is.Empty);
			Assert.That(morpher.ParseWord("basɯd"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("bas"), "disj");
		}

		[Test]
		public void FreeFluctuation()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var voicelessCons = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("vd-").Value;
			var alvStop = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident-")
				.Symbol("alveolar")
				.Symbol("nasal-").Value;
			var d = FeatureStruct.New(Language.PhoneticFeatureSystem)
				.Symbol(HCFeatureSystem.Segment)
				.Symbol("cons+")
				.Symbol("strident-")
				.Symbol("del_rel-")
				.Symbol("alveolar")
				.Symbol("nasal-")
				.Symbol("vd+").Value;

			var edSuffix = new AffixProcessRule
							{
								Name = "ed_suffix",
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
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

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("tazd"), "free PAST");
			AssertMorphsEqual(morpher.ParseWord("tast"), "free PAST");
			AssertMorphsEqual(morpher.ParseWord("badɯd"), "disj PAST");
			AssertMorphsEqual(morpher.ParseWord("batɯd"), "disj PAST");
		}

		[Test]
		public void StemNames()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

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

			var sSuffix = new AffixProcessRule
							{
								Name = "s_suffix",
								Gloss = "PRES",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
								OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
									.Feature("head").EqualTo(head => head
										.Feature("tense").EqualTo("pres")).Value
							};
			Morphophonemic.MorphologicalRules.Add(sSuffix);
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+s")}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("sadɯd"), "stemname PAST");
			Assert.That(morpher.ParseWord("sanɯd"), Is.Empty);
			Assert.That(morpher.ParseWord("sads"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("sans"), "stemname PRES");
			Assert.That(morpher.ParseWord("sad"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("san"), "stemname");
		}

		[Test]
		public void BoundRootAllomorph()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var edSuffix = new AffixProcessRule
							{
								Name = "ed_suffix",
								Gloss = "PAST",
								RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value
							};
			Morphophonemic.MorphologicalRules.Add(edSuffix);
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
										{
											Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
											Rhs = {new CopyFromInput("1"), new InsertShape(Table3, "+ɯd")}
										});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			Assert.That(morpher.ParseWord("dag"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("dagɯd"), "bound PAST");
		}

		[Test]
		public void AllomorphEnvironments()
		{
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol("voc+").Value;

			LexEntry headEntry = Entries["32"];
			Pattern<Word, ShapeNode> envPattern = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value;
			var env = new AllomorphEnvironment(SpanFactory, null, envPattern);
			headEntry.PrimaryAllomorph.RequiredEnvironments.Add(env);

			var word = new Word(headEntry.PrimaryAllomorph, FeatureStruct.New().Value);

			ShapeNode node = word.Shape.Last;
			LexEntry nonHeadEntry = Entries["40"];
			word.Shape.AddRange(nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().CloneItems());
			Annotation<ShapeNode> nonHeadMorph = word.MarkMorph(word.Shape.GetNodes(node.Next, word.Shape.Last), nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsMatch(word), Is.True);

			word.RemoveMorph(nonHeadMorph);

			nonHeadEntry = Entries["41"];
			word.Shape.AddRange(nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().CloneItems());
			nonHeadMorph = word.MarkMorph(word.Shape.GetNodes(node.Next, word.Shape.Last), nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsMatch(word), Is.False);

			headEntry.PrimaryAllomorph.RequiredEnvironments.Clear();

			env = new AllomorphEnvironment(SpanFactory, envPattern, null);
			headEntry.PrimaryAllomorph.RequiredEnvironments.Add(env);

			word.RemoveMorph(nonHeadMorph);

			node = word.Shape.First;
			nonHeadEntry = Entries["40"];
			word.Shape.AddRangeAfter(word.Shape.Begin, nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().CloneItems());
			nonHeadMorph = word.MarkMorph(word.Shape.GetNodes(word.Shape.First, node.Prev), nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsMatch(word), Is.True);

			word.RemoveMorph(nonHeadMorph);

			nonHeadEntry = Entries["41"];
			word.Shape.AddRangeAfter(word.Shape.Begin, nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().CloneItems());
			word.MarkMorph(word.Shape.GetNodes(word.Shape.First, node.Prev), nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsMatch(word), Is.False);
		}
	}
}
