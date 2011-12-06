using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.HermitCrab;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace HermitCrabTest
{
	public class AffixProcessRuleTest : HermitCrabTestBase
	{
		[Test]
		public void SuffixRules()
		{
			var any = new FeatureStruct();
			var strident = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("strident+").Value;
			var voicelessCons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("vd-").Value;
			var alvStop = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("strident-").Symbol("del_rel-").Symbol("alveolar").Symbol("nasal-").Symbol("asp-").Value;
			var d = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Symbol("strident-").Symbol("del_rel-").Symbol("alveolar").Symbol("nasal-").Symbol("vd+").Value;
			var unasp = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("asp-").Value;
			var cons = FeatureStruct.New(Morpher.PhoneticFeatureSystem).Symbol("cons+").Value;

			var mrule1 = new AffixProcessRule("s_suffix", SpanFactory);
			
			var allomorph = new AffixProcessAllomorph("allo1");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Annotation(HCFeatureSystem.SegmentType, strident).Value);

			Shape shape;
			allomorph.Rhs.Add(new CopyFromInput(0));
			Table3.ToShape("ɯz", out shape);
			allomorph.Rhs.Add(new InsertShape(shape));
			mrule1.AddAllomorph(allomorph);

			allomorph = new AffixProcessAllomorph("allo2");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Annotation(HCFeatureSystem.SegmentType, voicelessCons).Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			Table3.ToShape("s", out shape);
			allomorph.Rhs.Add(new InsertShape(shape));
			mrule1.AddAllomorph(allomorph);

			allomorph = new AffixProcessAllomorph("allo3");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			Table3.ToShape("z", out shape);
			allomorph.Rhs.Add(new InsertShape(shape));
			mrule1.AddAllomorph(allomorph);

			Morphophonemic.AddMorphologicalRule(mrule1);

			var mrule2 = new AffixProcessRule("ed_suffix", SpanFactory);

			allomorph = new AffixProcessAllomorph("allo4");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Value);
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, alvStop).Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			allomorph.Rhs.Add(new CopyFromInput(1));
			Table3.ToShape("+ɯd", out shape);
			allomorph.Rhs.Add(new InsertShape(shape));
			mrule2.AddAllomorph(allomorph);

			allomorph = new AffixProcessAllomorph("allo5");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Annotation(HCFeatureSystem.SegmentType, voicelessCons).Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			Table3.ToShape("+t", out shape);
			allomorph.Rhs.Add(new InsertShape(shape));
			mrule2.AddAllomorph(allomorph);

			allomorph = new AffixProcessAllomorph("allo6");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			Table3.ToShape("+", out shape);
			allomorph.Rhs.Add(new InsertShape(shape));
			allomorph.Rhs.Add(new InsertShapeNodeFromConstraint(new Constraint<Word, ShapeNode>(HCFeatureSystem.SegmentType, d)));
			mrule2.AddAllomorph(allomorph);

			Morphophonemic.AddMorphologicalRule(mrule2);

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSymbolDefinition("t").FeatureStruct).Value;
			var prule1 = new StandardPhonologicalRule("rule1", SpanFactory, lhs);

			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			prule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			Allophonic.AddPhonologicalRule(prule1);

			IEnumerable<Word> output = Morpher.MorphAndLookupWord("sagz");
			//Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			//Assert.IsFalse(mrule2.AnalysisRule.Apply(input, out output));
			//Assert.IsTrue(mrule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]g", output.Single().ToString());

			var input = new Word(Morphophonemic, "sag");
			Assert.IsTrue(mrule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Allophonic;
			Assert.IsFalse(prule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("sagz", input.ToString());

			input = new Word(Surface, "sagd") { Stratum = Allophonic };
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsTrue(mrule2.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]g", output.Single().ToString());
			Assert.IsFalse(mrule1.AnalysisRule.Apply(input, out output));

			input = new Word(Morphophonemic, "sag");
			Assert.IsTrue(mrule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Allophonic;
			Assert.IsFalse(prule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("sag+?d", input.ToString());

			input = new Word(Surface, "sasɯz") { Stratum = Allophonic };
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsFalse(mrule2.AnalysisRule.Apply(input, out output));
			Assert.IsTrue(mrule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]s", output.Single().ToString());

			input = new Word(Morphophonemic, "sas");
			Assert.IsTrue(mrule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Allophonic;
			Assert.IsFalse(prule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("sasɯz", input.ToString());

			input = new Word(Surface, "sast") { Stratum = Allophonic };
			Assert.IsTrue(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsTrue(mrule2.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]s", output.Single().ToString());
			Assert.IsFalse(mrule1.AnalysisRule.Apply(input, out output));

			input = new Word(Morphophonemic, "sas");
			Assert.IsTrue(mrule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Allophonic;
			Assert.IsFalse(prule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("sas+?[t(tʰ)]", input.ToString());

			input = new Word(Surface, "sazd") { Stratum = Allophonic };
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsTrue(mrule2.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]z", output.Single().ToString());
			Assert.IsFalse(mrule1.AnalysisRule.Apply(input, out output));

			input = new Word(Morphophonemic, "saz");
			Assert.IsTrue(mrule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Allophonic;
			Assert.IsFalse(prule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("saz+?d", input.ToString());

			input = new Word(Surface, "sagɯs") { Stratum = Allophonic };
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsFalse(mrule2.AnalysisRule.Apply(input, out output));
			Assert.IsFalse(mrule1.AnalysisRule.Apply(input, out output));

			input = new Word(Surface, "sags") { Stratum = Allophonic };
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsFalse(mrule2.AnalysisRule.Apply(input, out output));
			Assert.IsFalse(mrule1.AnalysisRule.Apply(input, out output));
		}
	}
}
