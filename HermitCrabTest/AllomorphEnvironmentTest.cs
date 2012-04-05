using System.Linq;
using NUnit.Framework;
using SIL.Collections;
using SIL.HermitCrab;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace HermitCrabTest
{
	public class AllomorphEnvironmentTest : HermitCrabTestBase
	{
		[Test]
		public void EntryEnvironments()
		{
			var vowel = FeatureStruct.New(Language.PhoneticFeatureSystem).Symbol("voc+").Value;

			LexEntry headEntry = Entries["32"];
			Pattern<Word, ShapeNode> leftEnv = Pattern<Word, ShapeNode>.New().Value;
			Pattern<Word, ShapeNode> rightEnv = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value;
			var env = new AllomorphEnvironment(SpanFactory, leftEnv, rightEnv);
			headEntry.PrimaryAllomorph.RequiredEnvironments.Add(env);

			var word = new Word(headEntry.PrimaryAllomorph, FeatureStruct.New().Value);

			ShapeNode node = word.Shape.Last;
			LexEntry nonHeadEntry = Entries["40"];
			word.Shape.AddRange(nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().DeepClone());
			Annotation<ShapeNode> nonHeadMorph = word.Annotations.Add(node.Next, word.Shape.Last, FeatureStruct.New()
				.Symbol(HCFeatureSystem.Morph)
				.Feature(HCFeatureSystem.Allomorph).EqualTo(nonHeadEntry.PrimaryAllomorph.ID).Value);
			word.Allomorphs.Add(nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsMatch(word), Is.True);

			nonHeadMorph.Remove(false);
			word.Allomorphs.Remove(nonHeadEntry.PrimaryAllomorph);

			nonHeadEntry = Entries["41"];
			word.Shape.AddRange(nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().DeepClone());
			nonHeadMorph = word.Annotations.Add(node.Next, word.Shape.Last, FeatureStruct.New()
				.Symbol(HCFeatureSystem.Morph)
				.Feature(HCFeatureSystem.Allomorph).EqualTo(nonHeadEntry.PrimaryAllomorph.ID).Value);
			word.Allomorphs.Add(nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsMatch(word), Is.False);

			headEntry.PrimaryAllomorph.RequiredEnvironments.Clear();

			leftEnv = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value;
			rightEnv = Pattern<Word, ShapeNode>.New().Value;
			env = new AllomorphEnvironment(SpanFactory, leftEnv, rightEnv);
			headEntry.PrimaryAllomorph.RequiredEnvironments.Add(env);

			nonHeadMorph.Remove(false);
			word.Allomorphs.Remove(nonHeadEntry.PrimaryAllomorph);

			node = word.Shape.First;
			nonHeadEntry = Entries["40"];
			word.Shape.AddRangeAfter(word.Shape.Begin, nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().DeepClone());
			nonHeadMorph = word.Annotations.Add(word.Shape.First, node.Prev, FeatureStruct.New()
				.Symbol(HCFeatureSystem.Morph)
				.Feature(HCFeatureSystem.Allomorph).EqualTo(nonHeadEntry.PrimaryAllomorph.ID).Value);
			word.Allomorphs.Add(nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsMatch(word), Is.True);

			nonHeadMorph.Remove(false);
			word.Allomorphs.Remove(nonHeadEntry.PrimaryAllomorph);

			nonHeadEntry = Entries["41"];
			word.Shape.AddRangeAfter(word.Shape.Begin, nonHeadEntry.PrimaryAllomorph.Shape.AsEnumerable().DeepClone());
			word.Annotations.Add(word.Shape.First, node.Prev, FeatureStruct.New()
				.Symbol(HCFeatureSystem.Morph)
				.Feature(HCFeatureSystem.Allomorph).EqualTo(nonHeadEntry.PrimaryAllomorph.ID).Value);
			word.Allomorphs.Add(nonHeadEntry.PrimaryAllomorph);

			Assert.That(env.IsMatch(word), Is.False);
		}
	}
}
