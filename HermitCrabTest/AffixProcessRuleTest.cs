using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;
using SIL.HermitCrab;

namespace HermitCrabTest
{
	public class AffixProcessRuleTest : HermitCrabTestBase
	{
		[Test]
		public void SuffixRules()
		{
			var any = new FeatureStruct();
			var strident = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("strident+").Value;
			var voicelessCons = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("vd-").Value;
			var alvStop = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("strident-").Symbol("del_rel-").Symbol("alveolar").Symbol("nasal-").Symbol("asp-").Value;
			var d = FeatureStruct.New(FeatSys).Symbol("cons+").Symbol("strident-").Symbol("del_rel-").Symbol("alveolar").Symbol("nasal-").Symbol("vd+").Value;
			var unasp = FeatureStruct.New(FeatSys).Symbol("asp-").Value;
			var cons = FeatureStruct.New(FeatSys).Symbol("cons+").Value;

			var mrule1 = new AffixProcessRule("s_suffix", "s_suffix", SpanFactory);
			
			var allomorph = new AffixProcessAllomorph("allo1", "allo1");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Annotation(HCFeatureSystem.SegmentType, strident).Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			allomorph.Rhs.Add(new InsertShape(Table3.ToShape("ɯz")));
			mrule1.AddAllomorph(allomorph);

			allomorph = new AffixProcessAllomorph("allo2", "allo2");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Annotation(HCFeatureSystem.SegmentType, voicelessCons).Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			allomorph.Rhs.Add(new InsertShape(Table3.ToShape("s")));
			mrule1.AddAllomorph(allomorph);

			allomorph = new AffixProcessAllomorph("allo3", "allo3");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			allomorph.Rhs.Add(new InsertShape(Table3.ToShape("z")));
			mrule1.AddAllomorph(allomorph);

			var mrule2 = new AffixProcessRule("ed_suffix", "ed_suffix", SpanFactory);

			allomorph = new AffixProcessAllomorph("allo4", "allo4");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Value);
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, alvStop).Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			allomorph.Rhs.Add(new CopyFromInput(1));
			allomorph.Rhs.Add(new InsertShape(Table3.ToShape("+ɯd")));
			mrule2.AddAllomorph(allomorph);

			allomorph = new AffixProcessAllomorph("allo5", "allo5");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Annotation(HCFeatureSystem.SegmentType, voicelessCons).Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			allomorph.Rhs.Add(new InsertShape(Table3.ToShape("+t")));
			mrule2.AddAllomorph(allomorph);

			allomorph = new AffixProcessAllomorph("allo6", "allo6");
			allomorph.Lhs.Add(Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, any).OneOrMore.Value);

			allomorph.Rhs.Add(new CopyFromInput(0));
			allomorph.Rhs.Add(new InsertShape(Table3.ToShape("+")));
			allomorph.Rhs.Add(new InsertShapeNodeFromConstraint(new Constraint<Word, ShapeNode>(HCFeatureSystem.SegmentType, d)));
			mrule2.AddAllomorph(allomorph);

			var lhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, Table3.GetSegmentDefinition("t").FeatureStruct).Value;
			var prule1 = new StandardPhonologicalRule("rule1", "rule1", SpanFactory, 0, Direction.LeftToRight, ApplicationMode.Iterative, lhs);

			var rhs = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			var leftEnv = Expression<Word, ShapeNode>.New().Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Expression<Word, ShapeNode>.New().Value;
			prule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			IEnumerable<Word> output;
			var input = new Word(Surface, Mode.Analysis, "sagz") {Stratum = Allophonic};
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsFalse(mrule2.AnalysisRule.Apply(input, out output));
			Assert.IsTrue(mrule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]g", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "sag");
			Assert.IsTrue(mrule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Allophonic;
			Assert.IsFalse(prule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("sagz", input.ToString());

			input = new Word(Surface, Mode.Analysis, "sagd") { Stratum = Allophonic };
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsTrue(mrule2.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]g", output.Single().ToString());
			Assert.IsFalse(mrule1.AnalysisRule.Apply(input, out output));

			input = new Word(Morphophonemic, Mode.Synthesis, "sag");
			Assert.IsTrue(mrule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Allophonic;
			Assert.IsFalse(prule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("sag+?d", input.ToString());

			input = new Word(Surface, Mode.Analysis, "sasɯz") { Stratum = Allophonic };
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsFalse(mrule2.AnalysisRule.Apply(input, out output));
			Assert.IsTrue(mrule1.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]s", output.Single().ToString());

			input = new Word(Morphophonemic, Mode.Synthesis, "sas");
			Assert.IsTrue(mrule1.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Allophonic;
			Assert.IsFalse(prule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("sasɯz", input.ToString());

			input = new Word(Surface, Mode.Analysis, "sast") { Stratum = Allophonic };
			Assert.IsTrue(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsTrue(mrule2.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]s", output.Single().ToString());
			Assert.IsFalse(mrule1.AnalysisRule.Apply(input, out output));

			input = new Word(Morphophonemic, Mode.Synthesis, "sas");
			Assert.IsTrue(mrule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Allophonic;
			Assert.IsFalse(prule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("sas+?[t(tʰ)]", input.ToString());

			input = new Word(Surface, Mode.Analysis, "sazd") { Stratum = Allophonic };
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsTrue(mrule2.AnalysisRule.Apply(input, out output));
			Assert.AreEqual("s[a(a̘)]z", output.Single().ToString());
			Assert.IsFalse(mrule1.AnalysisRule.Apply(input, out output));

			input = new Word(Morphophonemic, Mode.Synthesis, "saz");
			Assert.IsTrue(mrule2.SynthesisRule.Apply(input, out output));
			input = output.Single();
			input.Stratum = Allophonic;
			Assert.IsFalse(prule1.SynthesisRule.Apply(input, out output));
			input.Stratum = Surface;
			Assert.AreEqual("saz+?d", input.ToString());

			input = new Word(Surface, Mode.Analysis, "sagɯs") { Stratum = Allophonic };
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsFalse(mrule2.AnalysisRule.Apply(input, out output));
			Assert.IsFalse(mrule1.AnalysisRule.Apply(input, out output));

			input = new Word(Surface, Mode.Analysis, "sags") { Stratum = Allophonic };
			Assert.IsFalse(prule1.AnalysisRule.Apply(input, out output));
			input.Stratum = Morphophonemic;
			Assert.IsFalse(mrule2.AnalysisRule.Apply(input, out output));
			Assert.IsFalse(mrule1.AnalysisRule.Apply(input, out output));
		}
	}
}
