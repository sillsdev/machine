using System.Linq;
using NUnit.Framework;
using SIL.Collections;
using SIL.HermitCrab.MorphologicalRules;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.Tests
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
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
			{
				Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
				Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "+ɯd")}
			});
			Morphophonemic.MorphologicalRules.Add(edSuffix);

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
			var d = FeatureStruct.New(Language.PhonologicalFeatureSystem)
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
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
			{
				Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
				Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "+t")}
			});
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
			{
				Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
				Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "+"), new InsertSimpleContext(d)}
			});
			Morphophonemic.MorphologicalRules.Add(edSuffix);

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("tazd"), "free PAST");
			AssertMorphsEqual(morpher.ParseWord("tast"), "free PAST");
			AssertMorphsEqual(morpher.ParseWord("tazt"), "free PAST");
			AssertMorphsEqual(morpher.ParseWord("tasd"), "free PAST");
		}

		[Test]
		public void StemNames()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

			var edSuffix = new AffixProcessRule
			{
				Name = "ed_suffix",
				Gloss = "1",
				RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
				OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
					.Feature(Head).EqualTo(head => head
						.Feature("pers").EqualTo("1")).Value
			};
			edSuffix.Allomorphs.Add(new AffixProcessAllomorph
			{
				Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
				Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "+ɯd")}
			});
			Morphophonemic.MorphologicalRules.Add(edSuffix);

			var tSuffix = new AffixProcessRule
			{
				Name = "t_suffix",
				Gloss = "2",
				RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
				OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
					.Feature(Head).EqualTo(head => head
						.Feature("pers").EqualTo("2")).Value
			};
			tSuffix.Allomorphs.Add(new AffixProcessAllomorph
			{
				Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
				Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "+t")}
			});
			Morphophonemic.MorphologicalRules.Add(tSuffix);

			var sSuffix = new AffixProcessRule
			{
				Name = "s_suffix",
				Gloss = "3",
				RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
				OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem)
					.Feature(Head).EqualTo(head => head
						.Feature("pers").EqualTo("3")).Value
			};
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
			{
				Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
				Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "+s")}
			});
			Morphophonemic.MorphologicalRules.Add(sSuffix);

			var morpher = new Morpher(SpanFactory, TraceManager, Language);

			AssertMorphsEqual(morpher.ParseWord("sanɯd"));
			AssertMorphsEqual(morpher.ParseWord("sant"));
			AssertMorphsEqual(morpher.ParseWord("sans"));
			AssertMorphsEqual(morpher.ParseWord("san"), "stemname");

			AssertMorphsEqual(morpher.ParseWord("sadɯd"), "stemname 1");
			AssertMorphsEqual(morpher.ParseWord("sadt"), "stemname 2");
			AssertMorphsEqual(morpher.ParseWord("sads"));
			AssertMorphsEqual(morpher.ParseWord("sad"));

			AssertMorphsEqual(morpher.ParseWord("sapɯd"), "stemname 1");
			AssertMorphsEqual(morpher.ParseWord("sapt"));
			AssertMorphsEqual(morpher.ParseWord("saps"), "stemname 3");
			AssertMorphsEqual(morpher.ParseWord("sap"));
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
				Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "+ɯd")}
			});

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			Assert.That(morpher.ParseWord("dag"), Is.Empty);
			AssertMorphsEqual(morpher.ParseWord("dagɯd"), "bound PAST");
		}

		[Test]
		public void AllomorphEnvironments()
		{
			var vowel = FeatureStruct.New(Language.PhonologicalFeatureSystem).Symbol("voc+").Value;

			LexEntry headEntry = Entries["32"];
			Pattern<Word, ShapeNode> envPattern = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value;
			var env = new AllomorphEnvironment(SpanFactory, ConstraintType.Require, null, envPattern);
			headEntry.PrimaryAllomorph.Environments.Add(env);

			var word = new Word(headEntry.PrimaryAllomorph, FeatureStruct.New().Value);

			ShapeNode node = word.Shape.Last;
			LexEntry nonHeadEntry = Entries["40"];
			word.Shape.AddRange(nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().DeepClone());
			Annotation<ShapeNode> nonHeadMorph = word.MarkMorph(word.Shape.GetNodes(node.Next, word.Shape.Last), nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsWordValid(word), Is.True);

			word.RemoveMorph(nonHeadMorph);

			nonHeadEntry = Entries["41"];
			word.Shape.AddRange(nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().DeepClone());
			nonHeadMorph = word.MarkMorph(word.Shape.GetNodes(node.Next, word.Shape.Last), nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsWordValid(word), Is.False);

			headEntry.PrimaryAllomorph.Environments.Clear();

			env = new AllomorphEnvironment(SpanFactory, ConstraintType.Require, envPattern, null);
			headEntry.PrimaryAllomorph.Environments.Add(env);

			word.RemoveMorph(nonHeadMorph);

			node = word.Shape.First;
			nonHeadEntry = Entries["40"];
			word.Shape.AddRangeAfter(word.Shape.Begin, nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().DeepClone());
			nonHeadMorph = word.MarkMorph(word.Shape.GetNodes(word.Shape.First, node.Prev), nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsWordValid(word), Is.True);

			word.RemoveMorph(nonHeadMorph);

			nonHeadEntry = Entries["41"];
			word.Shape.AddRangeAfter(word.Shape.Begin, nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().DeepClone());
			word.MarkMorph(word.Shape.GetNodes(word.Shape.First, node.Prev), nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsWordValid(word), Is.False);
		}

		[Test]
		public void PartialEntry()
		{
			var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
			var nominalizer = new AffixProcessRule
			{
				Name = "nominalizer",
				Gloss = "NOM",
				RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
				OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value
			};
			nominalizer.Allomorphs.Add(new AffixProcessAllomorph
			{
				Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
				Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "v")}
			});
			Morphophonemic.MorphologicalRules.Add(nominalizer);

			var morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("pi"), "54");
			AssertMorphsEqual(morpher.ParseWord("piv"), "54 NOM");

			Morphophonemic.MorphologicalRules.Clear();

			var sSuffix = new AffixProcessRule
			{
				Name = "s_suffix",
			    Gloss = "PAST",
				RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
			};
			sSuffix.Allomorphs.Add(new AffixProcessAllomorph
			{
				Lhs = {Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value},
				Rhs = {new CopyFromInput("1"), new InsertSegments(Table3, "s")}
			});

			var verbTemplate = new AffixTemplate {Name = "verb", RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value};
			verbTemplate.Slots.Add(new AffixTemplateSlot(sSuffix) {Optional = true});
			Morphophonemic.AffixTemplates.Add(verbTemplate);

			morpher = new Morpher(SpanFactory, TraceManager, Language);
			AssertMorphsEqual(morpher.ParseWord("pi"), "54");
			AssertMorphsEqual(morpher.ParseWord("pis"));
		}
	}
}
