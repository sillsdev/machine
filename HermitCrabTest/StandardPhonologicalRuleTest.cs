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
		private FeatureSystem _phoneticFeatSys;
		private CharacterDefinitionTable _table1;
		private CharacterDefinitionTable _table2;
		private CharacterDefinitionTable _table3;
		private int _delReapplications;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			_delReapplications = 0;
			_spanFactory = new SpanFactory<PhoneticShapeNode>((x, y) => x.CompareTo(y), (start, end) => start.GetNodes(end).Count(), true);
			_phoneticFeatSys = FeatureSystem.With
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
					.Symbol("velar"))
				.StringFeature("strRep").Value;

			_table1 = new CharacterDefinitionTable("table1", "table1", _spanFactory, _phoneticFeatSys);
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

			_table2 = new CharacterDefinitionTable("table2", "table2", _spanFactory, _phoneticFeatSys);
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

			_table3 = new CharacterDefinitionTable("table3", "table3", _spanFactory, _phoneticFeatSys);
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
		}

		private void AddSegDef(CharacterDefinitionTable table, string strRep, params string[] symbols)
		{
			var fs = new FeatureStruct();
			foreach (string symbolID in symbols)
			{
				FeatureSymbol symbol = _phoneticFeatSys.GetSymbol(symbolID);
				fs.AddValue(symbol.Feature, new SymbolicFeatureValue(symbol));
			}
			table.AddSegmentDefinition(strRep, fs);
		}

		[Test]
		public void SimpleRules()
		{
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table1.GetSegmentDefinition("t").FeatureStruct).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("asp+").Value).Value;
			var leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Value).Value;
			var rightEnv = new Expression<PhoneticShapeNode>();
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table3.GetSegmentDefinition("p").FeatureStruct).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("asp+").Value).Value;
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Value).Value;
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
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value).Value;
			var rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("back+").Symbol("round+").Value).Value;
			var leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value).Value;
			var rightEnv = new Expression<PhoneticShapeNode>();
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
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Value).Value;
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
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Value).Value;
			var rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("vd-").Symbol("asp-").Value).Value;
			var leftEnv = new Expression<PhoneticShapeNode>();
			var rightEnv = Expression<PhoneticShapeNode>.With.RightSideOfInput.Value;
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

			rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Value)
				.RightSideOfInput.Value;
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

			rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.With.LeftSideOfInput.Value;
			rightEnv = new Expression<PhoneticShapeNode>();
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

			rule = new StandardPhonologicalRule("rule", "rule", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.With
				.LeftSideOfInput
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value).Value;
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
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("back+").Symbol("round+").Value).Value;
			var leftEnv = new Expression<PhoneticShapeNode>();
			var rightEnv = Expression<PhoneticShapeNode>.With
				.Group(g => g
	            	.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value)
	            	.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Value)).Range(1, 2)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Value)
				.Group(g => g
					.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value)
					.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Value)).Range(1, 2)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value).Value;
			rightEnv = new Expression<PhoneticShapeNode>();
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

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value).LazyRange(0, 2).Value;
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
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("back+").Symbol("round+").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("back+").Symbol("round+").Value).Value;
			var leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value).Value;
			var rightEnv = new Expression<PhoneticShapeNode>();
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var shape = _table1.ToPhoneticShape("buuubuuu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu[iuyɯ][iuyɯ]bu[iuyɯ][iuyɯ]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("buiibuii", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buuubuuu", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("alveolar").Symbol("del_rel-").Symbol("asp-").Symbol("vd-").Symbol("strident-").Value).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value).Value;
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
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("back+").Symbol("round+").Value).Value;
			var leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value)
				.Annotation("boundary", _table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			var rightEnv = new Expression<PhoneticShapeNode>();
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

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("back-").Symbol("round-").Value).Value;
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("boundary", _table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round-").Value).Value;
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

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("back+").Symbol("round+").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Value).Value;
			rightEnv = new Expression<PhoneticShapeNode>();
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

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("back-").Symbol("round-").Value).Value;
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("back-").Symbol("round-").Value).Value;
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

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table3.GetSegmentDefinition("i").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = new Expression<PhoneticShapeNode>();
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table3.GetSegmentDefinition("b").FeatureStruct).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Value).Value;
			var rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table3.GetSegmentDefinition("a").FeatureStruct).Value;
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("boundary", _table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation("segment", _table3.GetSegmentDefinition("b").FeatureStruct).Value;
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

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table3.GetSegmentDefinition("u").FeatureStruct).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = new Expression<PhoneticShapeNode>();
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table3.GetSegmentDefinition("b").FeatureStruct).Value;
			rightEnv = new Expression<PhoneticShapeNode>();
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("round-").Value).Value;
			rule2 = new StandardPhonologicalRule("rule2", "rule2", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("back+").Symbol("low+").Symbol("high-").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table3.GetSegmentDefinition("b").FeatureStruct)
				.Annotation("boundary", _table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			rightEnv = new Expression<PhoneticShapeNode>();
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

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Symbol("bilabial").Value)
				.Annotation("boundary", _table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Symbol("bilabial").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("vd-").Symbol("asp-").Value)
				.Annotation("boundary", _table3.GetBoundaryDefinition("+").FeatureStruct)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("vd-").Symbol("asp-").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value).Value;
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value).Value;
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

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Symbol("bilabial").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Symbol("bilabial").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("vd-").Symbol("asp-").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("vd-").Symbol("asp-").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value).Value;
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value).Value;
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

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("asp+").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("boundary", _table3.GetBoundaryDefinition("+").FeatureStruct).Value;
			rightEnv = new Expression<PhoneticShapeNode>();
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
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table1.GetSegmentDefinition("p").FeatureStruct).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("labiodental").Symbol("vd+").Symbol("strident+").Symbol("cont+").Value).Value;
			var leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value).Value;
			var rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value).Value;
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

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table1.GetSegmentDefinition("v").FeatureStruct).Value;
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
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value).Value;
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys)
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value).Value;
			var leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys)
					.Symbol("cons-").Symbol("voc+").Symbol("high+")
					.Feature("back").EqualToVariable("a")
					.Feature("round").EqualToVariable("b").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Value).Value;
			var rightEnv = new Expression<PhoneticShapeNode>();
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

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Symbol("nasal+").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys)
					.Feature("poa").EqualToVariable("a").Value).Value;
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys)
					.Symbol("cons+").Symbol("voc-")
					.Feature("poa").EqualToVariable("a").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("mbindiŋg", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[mnŋ]bi[mnŋ]di[mnŋ]g", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("nbinding", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("mbindiŋg", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys)
					.Symbol("cons+").Symbol("vd-").Symbol("cont-")
					.Feature("poa").EqualToVariable("a").Value).Value;
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("asp+").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys)
					.Symbol("cons+").Symbol("vd-").Symbol("cont-")
					.Feature("poa").EqualToVariable("a").Value)
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value).Value;
			rightEnv = new Expression<PhoneticShapeNode>();
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("asp-").Value).Value;
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = new Expression<PhoneticShapeNode>();
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("pipʰ", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[p(pʰ)]i[p(pʰ)]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pip", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("pipʰ", _table1.ToString(shape, ModeType.Synthesis, true));

			lhs = new Expression<PhoneticShapeNode>();
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table1.GetSegmentDefinition("f").FeatureStruct).Value;
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys)
					.Symbol("cons-").Symbol("voc+")
					.Feature("high").EqualToVariable("a")
					.Feature("back").EqualToVariable("b")
					.Feature("round").EqualToVariable("c").Value).Value;
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys)
					.Symbol("cons-").Symbol("voc+")
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

			lhs = new Expression<PhoneticShapeNode>();
			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys)
					.Symbol("cons+").Symbol("voc-").Symbol("velar").Symbol("vd-").Symbol("cont-").Symbol("nasal-")
					.Feature("asp").EqualToVariable("a").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys)
					.Symbol("cons+").Symbol("voc-").Symbol("velar").Symbol("vd+").Symbol("cont-").Symbol("nasal-")
					.Feature("asp").EqualToVariable("a").Value).Value;
			rightEnv = Expression<PhoneticShapeNode>.With
				.RightSideOfInput.Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("sagk", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("s[a(a̘)]gk?", _table3.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("sag", ModeType.Synthesis);
			var me = Assert.Throws<MorphException>(() => rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual(MorphException.MorphErrorType.UninstantiatedFeature, me.ErrorType);
		}

		[Test]
		public void EpenthesisRules()
		{
			var lhs = new Expression<PhoneticShapeNode>();
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, true, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Symbol("round-").Value).Value;
			var leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value).Value;
			var rightEnv = new Expression<PhoneticShapeNode>();
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

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, true, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", _table1.GetSegmentDefinition("i").FeatureStruct).Value;
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("biubiu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bi?ubi?u", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("biubiu", _table1.ToString(shape, ModeType.Synthesis, true));

			rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _delReapplications, Direction.LeftToRight, true, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("back-").Symbol("round-").Value).Value;
			leftEnv = Expression<PhoneticShapeNode>.With
				.LeftSideOfInput.Value;
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation("segment", FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("ipʰit", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("i?(pʰ)it", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pʰit", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("ipʰit", _table1.ToString(shape, ModeType.Synthesis, true));
		}
	}
}
