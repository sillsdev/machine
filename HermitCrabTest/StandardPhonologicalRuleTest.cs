using System.Linq;
using NUnit.Framework;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.HermitCrab;

namespace HermitCrabTest
{
	[TestFixture]
	public class StandardPhonologicalRuleTest
	{
		private SpanFactory<PhoneticShapeNode> _spanFactory;
		private FeatureSystem _featSys;
		private CharacterDefinitionTable _table1;
		private CharacterDefinitionTable _table2;
		private CharacterDefinitionTable _table3;
		private FeatureStruct _leftSide;
		private FeatureStruct _rightSide;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			_spanFactory = new SpanFactory<PhoneticShapeNode>((x, y) => x.CompareTo(y), (start, end) => start.GetNodes(end).Count(), true);
			_featSys = FeatureSystem.New
				.SymbolicFeature("voc", voc => voc
					.Symbol("voc+", "+")
					.Symbol("voc-", "-"))
				.SymbolicFeature("cons", cons => cons
					.Symbol("cons+", "+")
					.Symbol("cons-", "-"))
				.SymbolicFeature("high", high => high
					.Symbol("high+", "+")
					.Symbol("high-", "-"))
				.SymbolicFeature("low", low => low
					.Symbol("low+", "+")
					.Symbol("low-", "-"))
				.SymbolicFeature("back", back => back
					.Symbol("back+", "+")
					.Symbol("back-", "-"))
				.SymbolicFeature("round", round => round
					.Symbol("round+", "+")
					.Symbol("round-", "-"))
				.SymbolicFeature("vd", vd => vd
					.Symbol("vd+", "+")
					.Symbol("vd-", "-"))
				.SymbolicFeature("asp", asp => asp
					.Symbol("asp+", "+")
					.Symbol("asp-", "-"))
				.SymbolicFeature("del_rel", delrel => delrel
					.Symbol("del_rel+", "+")
					.Symbol("del_rel-", "-"))
				.SymbolicFeature("ATR", atr => atr
					.Symbol("ATR+", "+")
					.Symbol("ATR-", "-"))
				.SymbolicFeature("strident", strident => strident
					.Symbol("strident+", "+")
					.Symbol("strident-", "-"))
				.SymbolicFeature("cont", cont => cont
					.Symbol("cont+", "+")
					.Symbol("cont-", "-"))
				.SymbolicFeature("nasal", nasal => nasal
					.Symbol("nasal+", "+")
					.Symbol("nasal-", "-"))
				.SymbolicFeature("poa", poa => poa
					.Symbol("bilabial")
					.Symbol("labiodental")
					.Symbol("alveolar")
					.Symbol("velar")).Value;

			_table1 = new CharacterDefinitionTable("table1", "table1", _spanFactory);
			AddSegDef(_table1, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
			AddSegDef(_table1, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
			AddSegDef(_table1, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
			AddSegDef(_table1, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
			AddSegDef(_table1, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
			AddSegDef(_table1, "ɯ", "cons-", "voc+", "high+", "low-", "back+", "round-", "vd+");
			AddSegDef(_table1, "p", "cons+", "voc-", "bilabial", "vd-", "asp-", "strident-", "cont-", "nasal-");
			AddSegDef(_table1, "t", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident-", "cont-", "nasal-");
			AddSegDef(_table1, "k", "cons+", "voc-", "velar", "vd-", "asp-", "strident-", "cont-", "nasal-");
			AddSegDef(_table1, "ts", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel+", "strident+", "cont-", "nasal-");
			AddSegDef(_table1, "pʰ", "cons+", "voc-", "bilabial", "vd-", "asp+", "strident-", "cont-", "nasal-");
			AddSegDef(_table1, "tʰ", "cons+", "voc-", "alveolar", "vd-", "asp+", "del_rel-", "strident-", "cont-", "nasal-");
			AddSegDef(_table1, "kʰ", "cons+", "voc-", "velar", "vd-", "asp+", "strident-", "cont-", "nasal-");
			AddSegDef(_table1, "tsʰ", "cons+", "voc-", "alveolar", "vd-", "asp+", "del_rel+", "strident+", "cont-", "nasal-");
			AddSegDef(_table1, "b", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal-");
			AddSegDef(_table1, "d", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal-");
			AddSegDef(_table1, "g", "cons+", "voc-", "velar", "vd+", "cont-", "nasal-");
			AddSegDef(_table1, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
			AddSegDef(_table1, "n", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal+");
			AddSegDef(_table1, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
			AddSegDef(_table1, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(_table1, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(_table1, "f", "cons+", "voc-", "labiodental", "vd-", "asp-", "strident+", "cont+");
			AddSegDef(_table1, "v", "cons+", "voc-", "labiodental", "vd+", "asp-", "strident+", "cont+");

			_table2 = new CharacterDefinitionTable("table2", "table2", _spanFactory);
			AddSegDef(_table2, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
			AddSegDef(_table2, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
			AddSegDef(_table2, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
			AddSegDef(_table2, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
			AddSegDef(_table2, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
			AddSegDef(_table2, "p", "cons+", "voc-", "bilabial", "vd-");
			AddSegDef(_table2, "t", "cons+", "voc-", "alveolar", "vd-", "del_rel-", "strident-");
			AddSegDef(_table2, "k", "cons+", "voc-", "velar", "vd-");
			AddSegDef(_table2, "ts", "cons+", "voc-", "alveolar", "vd-", "del_rel+", "strident+");
			AddSegDef(_table2, "b", "cons+", "voc-", "bilabial", "vd+");
			AddSegDef(_table2, "d", "cons+", "voc-", "alveolar", "vd+", "strident-");
			AddSegDef(_table2, "g", "cons+", "voc-", "velar", "vd+");
			AddSegDef(_table2, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
			AddSegDef(_table2, "n", "cons+", "voc-", "alveolar", "vd+", "cont-", "nasal+");
			AddSegDef(_table2, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
			AddSegDef(_table2, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(_table2, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(_table2, "f", "cons+", "voc-", "labiodental", "vd-", "asp-", "strident+", "cont+");
			AddSegDef(_table2, "v", "cons+", "voc-", "labiodental", "vd+", "asp-", "strident+", "cont+");
			_table2.AddBoundaryDefinition("+");
			_table2.AddBoundaryDefinition("#");
			_table2.AddBoundaryDefinition("!");
			_table2.AddBoundaryDefinition(".");
			_table2.AddBoundaryDefinition("$");

			_table3 = new CharacterDefinitionTable("table3", "table3", _spanFactory);
			AddSegDef(_table3, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+", "ATR+", "cont+");
			AddSegDef(_table3, "a̘", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+", "ATR-", "cont+");
			AddSegDef(_table3, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+", "cont+");
			AddSegDef(_table3, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+", "cont+");
			AddSegDef(_table3, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+", "cont+");
			AddSegDef(_table3, "ɯ", "cons-", "voc+", "high+", "low-", "back+", "round-", "vd+", "cont+");
			AddSegDef(_table3, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+", "cont+");
			AddSegDef(_table3, "p", "cons+", "voc-", "bilabial", "vd-", "cont-", "nasal-");
			AddSegDef(_table3, "t", "cons+", "voc-", "alveolar", "vd-", "del_rel-", "strident-", "cont-", "nasal-");
			AddSegDef(_table3, "k", "cons+", "voc-", "velar", "vd-", "cont-", "nasal-");
			AddSegDef(_table3, "ts", "cons+", "voc-", "alveolar", "vd-", "del_rel+", "strident+", "cont-", "nasal-");
			AddSegDef(_table3, "b", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal-");
			AddSegDef(_table3, "d", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal-");
			AddSegDef(_table3, "g", "cons+", "voc-", "velar", "vd+", "cont-", "nasal-");
			AddSegDef(_table3, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
			AddSegDef(_table3, "n", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal+");
			AddSegDef(_table3, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
			AddSegDef(_table3, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(_table3, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(_table3, "f", "cons+", "voc-", "labiodental", "vd-", "asp-", "strident+", "cont+");
			AddSegDef(_table3, "v", "cons+", "voc-", "labiodental", "vd+", "asp-", "strident+", "cont+");
			_table3.AddBoundaryDefinition("+");
			_table3.AddBoundaryDefinition("#");
			_table3.AddBoundaryDefinition("!");
			_table3.AddBoundaryDefinition(".");

			_leftSide = FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.LeftSide).Value;
			_rightSide = FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.RightSide).Value;
		}

		private void AddSegDef(CharacterDefinitionTable table, string strRep, params string[] symbols)
		{
			var fs = new FeatureStruct();
			foreach (string symbolID in symbols)
			{
				FeatureSymbol symbol = _featSys.GetSymbol(symbolID);
				fs.AddValue(symbol.Feature, new SymbolicFeatureValue(symbol));
			}
			table.AddSegmentDefinition(strRep, fs);
		}

		[Test]
		public void SimpleRules()
		{
			var asp = FeatureStruct.New(_featSys).Symbol("asp+").Value;
			var nonCons = FeatureStruct.New(_featSys).Symbol("cons-").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table1.GetSegmentDefinition("t").FeatureStruct).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, asp).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, nonCons).Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("p").FeatureStruct).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, nonCons).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			PhoneticShape shape = _table1.ToPhoneticShape("pʰitʰ", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[p(pʰ)]i[t(tʰ)]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pʰit", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("pʰitʰ", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("pit", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("pʰitʰ", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("datʰ", ModeType.Analysis);
			Assert.IsFalse(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("da[t(tʰ)]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("dat", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("datʰ", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("gab", ModeType.Analysis);
			Assert.IsFalse(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("gab", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("gab", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gab", _table1.ToString(shape, ModeType.Synthesis, true));
		}

		[Test]
		public void LongDistanceRules()
		{
			var highVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(_featSys).Symbol("back+").Symbol("round+").Value;
			var rndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Value;
			var cons = FeatureStruct.New(_featSys).Symbol("cons+").Value;
			var lowVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, rndVowel)
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.SegmentType, lowVowel)
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			PhoneticShape shape = _table1.ToPhoneticShape("bubabu", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubab[iuyɯ]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubabu", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubabi", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.SegmentType, lowVowel)
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.SegmentType, rndVowel).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("bubabu", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("b[iuyɯ]babu", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubabu", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bɯbabu", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, true));
		}

		[Test]
		public void AnchorRules()
		{
			var cons = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Value;
			var vlUnasp = FeatureStruct.New(_featSys).Symbol("vd-").Symbol("asp-").Value;
			var vowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vlUnasp).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New.Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _rightSide).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			PhoneticShape shape = _table1.ToPhoneticShape("gap", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("g[a(a̘)][pb]", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("ga̘p", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gap", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("gab", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gap", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("ga+b", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ga+p", _table1.ToString(shape, ModeType.Synthesis, true));

			rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rightEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, vowel)
				.Annotation(HCFeatureSystem.SegmentType, cons)
				.Annotation(HCFeatureSystem.AnchorType, _rightSide).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("kab", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[kg][a(a̘)]b", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("gab", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("kab", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("ga+b", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ka+b", _table1.ToString(shape, ModeType.Synthesis, true));

			rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _leftSide).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("kab", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[kg][a(a̘)]b", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("gab", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("kab", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("ga+b", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ka+b", _table1.ToString(shape, ModeType.Synthesis, true));

			rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.AnchorType, _leftSide)
				.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("gap", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("g[a(a̘)][pb]", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("ga̘p", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gap", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("gab", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gap", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("ga+b", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ga+p", _table1.ToString(shape, ModeType.Synthesis, true));
		}

		[Test]
		public void QuantifierRules()
		{
			var highVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(_featSys).Symbol("back+").Symbol("round+").Value;
			var cons = FeatureStruct.New(_featSys).Symbol("cons+").Value;
			var lowVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Value;
			var rndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New.Value;
			var rightEnv = Expression<PhoneticShapeNode>.New
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, lowVowel)).LazyRange(1, 2)
				.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, rndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, rndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, lowVowel)).LazyRange(1, 2)
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			PhoneticShape shape = _table1.ToPhoneticShape("bubu", ModeType.Analysis);
			Assert.IsFalse(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("b+ubu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("b+ubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubabu", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("b[iuyɯ]bab[iuyɯ]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubabu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubabi", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bɯbabu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bibabi", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bibabi", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubababu", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("b[iuyɯ]babab[iuyɯ]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubababi", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubababu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bibababu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubababu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubabababu", ModeType.Analysis);
			Assert.IsFalse(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabababu", _table1.ToRegexString(shape, ModeType.Analysis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRndVowel).Annotation(HCFeatureSystem.SegmentType, highVowel).LazyRange(0, 2).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("buuubuuu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu[iuyɯ][iuyɯ]bu[iuyɯ][iuyɯ]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("buiibuii", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buuubuuu", _table1.ToString(shape, ModeType.Synthesis, true));
		}

		[Test]
		public void MultipleSegmentRules()
		{
			var highVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(_featSys).Symbol("back+").Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;
			var t = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("alveolar").Symbol("del_rel-").Symbol("asp-").Symbol("vd-").Symbol("strident-").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRnd).Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRndVowel).Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var shape = _table1.ToPhoneticShape("buuubuuu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu[iuyɯ][iuyɯ]bu[iuyɯ][iuyɯ]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("buiibuii", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buuubuuu", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, t).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRndVowel).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("buuubuuu", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("but?[iuyɯ]t?[iuyɯ]but?[iuyɯ]t?[iuyɯ]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("buiibuii", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buuubuuu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buitibuiti", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buitibuiti", _table1.ToString(shape, ModeType.Synthesis, true));
		}

		[Test]
		public void BoundaryRules()
		{
			var highVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;
			var backRnd = FeatureStruct.New(_featSys).Symbol("back+").Symbol("round+").Value;
			var unbackUnrnd = FeatureStruct.New(_featSys).Symbol("back-").Symbol("round-").Value;
			var unbackUnrndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round-").Value;
			var backVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Value;
			var unrndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("round-").Value;
			var lowBack = FeatureStruct.New(_featSys).Symbol("back+").Symbol("low+").Symbol("high-").Value;
			var bilabialCons = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Symbol("bilabial").Value;
			var unvdUnasp = FeatureStruct.New(_featSys).Symbol("vd-").Symbol("asp-").Value;
			var vowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Value;
			var cons = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Value;
			var asp = FeatureStruct.New(_featSys).Symbol("asp+").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, backRndVowel)
				.Annotation(HCFeatureSystem.BoundaryType, _table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var shape = _table1.ToPhoneticShape("buub", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu[iuyɯ]b", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("bu+ib", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu+ub", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("buib", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buib", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, unbackUnrnd).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.BoundaryType, _table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, unbackUnrndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("biib", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("b[iuyɯ]ib", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("bu+ib", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bi+ib", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("buib", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buib", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRndVowel).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("buub", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu[iuyɯ]b", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("bu+ib", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu+ub", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("buib", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buub", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, unbackUnrnd).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, unbackUnrndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("biib", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("b[iuyɯ]ib", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("bu+ib", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bi+ib", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("buib", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("biib", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("i").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("b").FeatureStruct).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backVowel).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("a").FeatureStruct).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.BoundaryType, _table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("b").FeatureStruct).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("bab", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("i?b[a(a̘)uɯo]i?b", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("bu+ib", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ba+b", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("buib", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bub", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("u").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("b").FeatureStruct).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, unrndVowel).Value;
			rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, lowBack).Value;
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("b").FeatureStruct)
				.Annotation(HCFeatureSystem.BoundaryType, _table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("bab", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu?[a(a̘)iɯ]bu?", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("bu+ib", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("b+ab", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("buib", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bib", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons)
				.Annotation(HCFeatureSystem.BoundaryType, _table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp)
				.Annotation(HCFeatureSystem.BoundaryType, _table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("appa", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[a(a̘)][pb][pb][a(a̘)]", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("ab+ba", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ap+pa", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("abba", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("abba", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons)
				.Annotation(HCFeatureSystem.SegmentType, bilabialCons).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp)
				.Annotation(HCFeatureSystem.SegmentType, unvdUnasp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("appa", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[a(a̘)][pb][pb][a(a̘)]", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("ab+ba", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ab+ba", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("abba", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("appa", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.BoundaryType, _table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("pʰipʰ", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pip", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("pip", _table1.ToString(shape, ModeType.Synthesis, true));
		}

		[Test]
		public void CommonFeatureRules()
		{
			var vowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Value;
			var vdLabFric = FeatureStruct.New(_featSys).Symbol("labiodental").Symbol("vd+").Symbol("strident+").Symbol("cont+").Value;

			var lhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, _table1.GetSegmentDefinition("p").FeatureStruct).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vdLabFric).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var shape = _table1.ToPhoneticShape("buvu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu[pbmfv]u", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bupu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buvu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("b+ubu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("b+ubu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table1.GetSegmentDefinition("v").FeatureStruct).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("buvu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu[pbmfv]u", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bupu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buvu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("b+ubu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("b+ubu", _table1.ToString(shape, ModeType.Synthesis, true));
		}

		[Test]
		public void AlphaVariableRules()
		{
			var highVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var cons = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Value;
			var nasalCons = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Symbol("nasal+").Value;
			var voicelessStop = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("vd-").Symbol("cont-").Value;
			var asp = FeatureStruct.New(_featSys).Symbol("asp+").Value;
			var vowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Value;
			var unasp = FeatureStruct.New(_featSys).Symbol("asp-").Value;
			var k = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Symbol("velar").Symbol("vd-").Symbol("cont-").Symbol("nasal-").Value;
			var g = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Symbol("velar").Symbol("vd+").Symbol("cont-").Symbol("nasal-").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, highVowel)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value)
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var shape = _table1.ToPhoneticShape("bububu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bub[iuyɯ]b[iuyɯ]", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("bubibi", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bububu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("bubibu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bububu", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, nasalCons).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys)
					.Feature("poa").EqualToVariable("a").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, cons)
					.Feature("poa").EqualToVariable("a").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("mbindiŋg", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[mnŋ]bi[mnŋ]di[mnŋ]g", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("nbinding", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("mbindiŋg", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, voicelessStop)
					.Feature("poa").EqualToVariable("a").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, voicelessStop)
					.Feature("poa").EqualToVariable("a").Value)
				.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("pipʰ", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pip", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("pipʰ", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table1.GetSegmentDefinition("f").FeatureStruct).Value;
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, vowel)
					.Feature("high").EqualToVariable("a")
					.Feature("back").EqualToVariable("b")
					.Feature("round").EqualToVariable("c").Value).Value;
			rightEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, vowel)
					.Feature("high").EqualToVariable("a")
					.Feature("back").EqualToVariable("b")
					.Feature("round").EqualToVariable("c").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("buifibuifi", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("buif?ibuif?i", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("buiibuii", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buifibuifi", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, k)
					.Feature("asp").EqualToVariable("a").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, g)
					.Feature("asp").EqualToVariable("a").Value).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _rightSide).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("sagk", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("s[a(a̘)]gk?", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("sag", ModeType.Synthesis);
			var me = Assert.Throws<MorphException>(() => rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual(MorphErrorCode.UninstantiatedFeature, me.ErrorCode);
		}

		[Test]
		public void EpenthesisRules()
		{
			var highVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var highFrontUnrndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Symbol("round-").Value;
			var highBackRndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back+").Symbol("round+").Value;
			var cons = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Value;
			var vowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Value;
			var highBackRnd = FeatureStruct.New(_featSys).Symbol("high+").Symbol("back+").Symbol("round+").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, true, lhs);
			var rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var shape = _table1.ToPhoneticShape("buibui", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bui?bui?", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubui", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buibuiii", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buibu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buiiibui", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buibui", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buiiibuiii", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buibui", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, true, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table1.GetSegmentDefinition("i").FeatureStruct).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("biubiu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bi?ubi?u", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("biubiu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _leftSide).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("ipʰit", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("i?(pʰ)it", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pʰit", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ipʰit", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _rightSide).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("pʰiti", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("(pʰ)iti?", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pʰit", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("pʰiti", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highBackRndVowel).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("biubiu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bi?ubi?u", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("b+ubu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("b+iubiu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, true, lhs);
			rhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, highVowel)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, highVowel)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("biibuu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bii?buu?", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bibu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("biibuu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, true, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("biiibuii", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bii?i?bui?i?", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bibu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("biiibuii", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule1 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highBackRnd).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highBackRndVowel).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.New.Value;
			var rule2 = new StandardPhonologicalRule("rule3", "rule3", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table1.GetSegmentDefinition("t").FeatureStruct).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("butubu", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("but?[iuoyɯ]bu", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("buibu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("butubu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.RightToLeft, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _leftSide).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("ipʰit", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("i?(pʰ)it", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pʰit", ModeType.Synthesis);
			var me = Assert.Throws<MorphException>(() => rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual(MorphErrorCode.TooManySegs, me.ErrorCode);
		}

		[Test]
		public void DeletionRules()
		{
			var highFrontUnrndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("low-").Symbol("back-").Symbol("round-").Value;
			var highVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var cons = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Value;
			var highBackRndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back+").Symbol("round+").Value;
			var asp = FeatureStruct.New(_featSys).Symbol("asp+").Value;
			var nonCons = FeatureStruct.New(_featSys).Symbol("cons-").Value;
			var vowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Value;
			var voiced = FeatureStruct.New(_featSys).Symbol("vd+").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.New.Value;
			var leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var shape = _table1.ToPhoneticShape("bubu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bui?bui?", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubui", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buibu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buibui", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("bubu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bui?i?i?bui?i?i?", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubui", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buibu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buibui", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buiibuii", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("bubu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("i?i?bui?i?bu", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("buibu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("iibubu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ibubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Annotation(HCFeatureSystem.SegmentType, highFrontUnrndVowel).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("bubu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("i?i?bui?i?bu", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("buibu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buibu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("iibubu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highBackRndVowel).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("bubu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bui?i?bui?i?", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubui", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubui", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buibu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buibu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buibui", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buibui", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("buiibuii", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("b").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _leftSide).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.BoundaryType, _table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("u").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("b").FeatureStruct)
				.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("u").FeatureStruct).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.BoundaryType, _table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _rightSide).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, _table3.GetSegmentDefinition("t").FeatureStruct).Value;
			var rule3 = new StandardPhonologicalRule("rule3", "rule3", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, nonCons).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule3.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("b", ModeType.Analysis);
			Assert.IsFalse(rule3.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("b?bu?b?u?", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("b+ubu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule3.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("+", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, cons)
					.Feature("poa").EqualToVariable("a")
					.Feature("vd").EqualToVariable("b")
					.Feature("cont").EqualToVariable("c")
					.Feature("nasal").EqualToVariable("d").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, FeatureStruct.New(_featSys, cons)
					.Feature("poa").EqualToVariable("a")
					.Feature("vd").EqualToVariable("b")
					.Feature("cont").EqualToVariable("c")
					.Feature("nasal").EqualToVariable("d").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, voiced).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("aba", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[a(a̘)][pb]?[pb][a(a̘)]", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("ab+ba", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("a+ba", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table3.ToPhoneticShape("abba", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("aba", _table1.ToString(shape, ModeType.Synthesis, true));
		}

		[Test]
		public void DisjunctiveRules()
		{
			var stop = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("cont-").Value;
			var asp = FeatureStruct.New(_featSys).Symbol("asp+").Value;
			var unasp = FeatureStruct.New(_featSys).Symbol("asp-").Value;
			var highVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(_featSys).Symbol("back+").Symbol("round+").Value;
			var backRndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value;
			var cons = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Value;
			var highFrontVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Value;
			var frontRnd = FeatureStruct.New(_featSys).Symbol("back-").Symbol("round+").Value;
			var frontRndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round+").Value;
			var backUnrnd = FeatureStruct.New(_featSys).Symbol("back+").Symbol("round-").Value;
			var backUnrndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round-").Value;
			var frontUnrnd = FeatureStruct.New(_featSys).Symbol("back-").Symbol("round-").Value;
			var frontUnrndVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round-").Value;
			var p = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("cont-").Symbol("vd-").Symbol("asp-").Symbol("bilabial").Value;
			var vd = FeatureStruct.New(_featSys).Symbol("vd+").Value;
			var vowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Value;
			var voicelessStop = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("vd-").Symbol("cont-").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, stop).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			
			var rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, asp).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _leftSide).Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());
			
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var shape = _table1.ToPhoneticShape("pʰip", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pip", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("pʰip", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			
			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, backRndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, frontRnd).Value;
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, frontRndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backUnrnd).Value;
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, backUnrndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, frontUnrnd).Value;
			leftEnv = Expression<PhoneticShapeNode>.New
				.Annotation(HCFeatureSystem.SegmentType, frontUnrndVowel)
				.Group(g => g.Annotation(HCFeatureSystem.SegmentType, cons).Annotation(HCFeatureSystem.SegmentType, highFrontVowel)).LazyZeroOrMore
				.Annotation(HCFeatureSystem.SegmentType, cons).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("bububu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bub[iuyɯ]b[iuyɯ]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubibi", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bububu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubibu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bububu", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, stop).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);

			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _leftSide).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _rightSide).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("pʰip", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pip", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("pʰip", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, p).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);

			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vd).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, vowel).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.AnchorType, _rightSide).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("bubu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu[p(pʰ)b]u", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bupu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			shape = _table1.ToPhoneticShape("bubu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, voicelessStop).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);

			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, asp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, voicelessStop).Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, unasp).Value;
			leftEnv = Expression<PhoneticShapeNode>.New.Value;
			rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("ktʰb", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[k(kʰ)][t(tʰ)]b", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("ktb", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ktʰb", _table1.ToString(shape, ModeType.Synthesis, true));
		}

		[Test]
		public void MultipleApplicationRules()
		{
			var highVowel = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value;
			var backRnd = FeatureStruct.New(_featSys).Symbol("back+").Symbol("round+").Value;
			var i = FeatureStruct.New(_featSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Symbol("round-").Value;
			var cons = FeatureStruct.New(_featSys).Symbol("cons+").Symbol("voc-").Value;

			var lhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, highVowel).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, true, lhs);
			var rhs = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, backRnd).Value;
			var leftEnv = Expression<PhoneticShapeNode>.New.Annotation(HCFeatureSystem.SegmentType, i).Annotation(HCFeatureSystem.SegmentType, cons).Value;
			var rightEnv = Expression<PhoneticShapeNode>.New.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var shape = _table1.ToPhoneticShape("gigugu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("gig[iuyɯ]g[iuyɯ]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("gigigi", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gigugu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, 0, Direction.LeftToRight, false, lhs);
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("gigugi", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("gig[iuyɯ]gi", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("gigigi", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gigugi", _table1.ToString(shape, ModeType.Synthesis, true));
		}
	}
}
