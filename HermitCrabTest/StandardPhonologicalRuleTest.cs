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

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
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
				.SymbolicFeature("type", type => type
					.Symbol("seg", "Segment")
					.Symbol("bdry", "Boundary"))
				.StringFeature("strRep").Value;

			_table1 = new CharacterDefinitionTable("table1", "table1", _spanFactory, _phoneticFeatSys);
			AddSegDef(_table1, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
			AddSegDef(_table1, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
			AddSegDef(_table1, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
			AddSegDef(_table1, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
			AddSegDef(_table1, "uû", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
			AddSegDef(_table1, "ü", "cons-", "voc+", "high+", "low-", "back+", "round-", "vd+");
			AddSegDef(_table1, "p", "cons+", "voc-", "bilabial", "vd-", "asp-", "strident-");
			AddSegDef(_table1, "t", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident-");
			AddSegDef(_table1, "k", "cons+", "voc-", "velar", "vd-", "asp-", "strident-");
			AddSegDef(_table1, "ts", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel+", "strident+");
			AddSegDef(_table1, "pH", "cons+", "voc-", "bilabial", "vd-", "asp+", "strident-");
			AddSegDef(_table1, "tH", "cons+", "voc-", "alveolar", "vd-", "asp+", "del_rel-", "strident-");
			AddSegDef(_table1, "kH", "cons+", "voc-", "velar", "vd-", "asp+", "strident-");
			AddSegDef(_table1, "tsH", "cons+", "voc-", "alveolar", "vd-", "asp+", "del_rel+", "strident+");
			AddSegDef(_table1, "b", "cons+", "voc-", "bilabial", "vd+", "strident-");
			AddSegDef(_table1, "d", "cons+", "voc-", "alveolar", "vd+", "strident-");
			AddSegDef(_table1, "g", "cons+", "voc-", "velar", "vd+", "strident-");
			AddSegDef(_table1, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+");
			AddSegDef(_table1, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+");

			_table2 = new CharacterDefinitionTable("table2", "table2", _spanFactory, _phoneticFeatSys);
			AddSegDef(_table2, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
			AddSegDef(_table2, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
			AddSegDef(_table2, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
			AddSegDef(_table2, "uû", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
			AddSegDef(_table2, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
			AddSegDef(_table2, "p", "cons+", "voc-", "bilabial", "vd-");
			AddSegDef(_table2, "t", "cons+", "voc-", "alveolar", "vd-", "del_rel-", "strident-");
			AddSegDef(_table2, "k", "cons+", "voc-", "velar", "vd-");
			AddSegDef(_table2, "ts", "cons+", "voc-", "alveolar", "vd-", "del_rel+", "strident+");
			AddSegDef(_table2, "b", "cons+", "voc-", "bilabial", "vd+");
			AddSegDef(_table2, "d", "cons+", "voc-", "alveolar", "vd+", "strident-");
			AddSegDef(_table2, "g", "cons+", "voc-", "velar", "vd+");
			AddSegDef(_table2, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+");
			AddSegDef(_table2, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+");
			_table2.AddBoundaryDefinition("+");
			_table2.AddBoundaryDefinition("#");
			_table2.AddBoundaryDefinition("!");
			_table2.AddBoundaryDefinition(".");
			_table2.AddBoundaryDefinition("$xyz");

			_table3 = new CharacterDefinitionTable("table3", "table3", _spanFactory, _phoneticFeatSys);
			AddSegDef(_table3, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+", "ATR+");
			AddSegDef(_table3, "a'", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+", "ATR-");
			AddSegDef(_table3, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
			AddSegDef(_table3, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
			AddSegDef(_table3, "uû", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
			AddSegDef(_table3, "ü", "cons-", "voc+", "high+", "low-", "back+", "round-", "vd+");
			AddSegDef(_table3, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
			AddSegDef(_table3, "p", "cons+", "voc-", "bilabial", "vd-");
			AddSegDef(_table3, "t", "cons+", "voc-", "alveolar", "vd-", "del_rel-", "strident-");
			AddSegDef(_table3, "k", "cons+", "voc-", "velar", "vd-");
			AddSegDef(_table3, "ts", "cons+", "voc-", "alveolar", "vd-", "del_rel+", "strident+");
			AddSegDef(_table3, "b", "cons+", "voc-", "bilabial", "vd+");
			AddSegDef(_table3, "d", "cons+", "voc-", "alveolar", "vd+", "strident-");
			AddSegDef(_table3, "g", "cons+", "voc-", "velar", "vd+");
			AddSegDef(_table3, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+");
			AddSegDef(_table3, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+");
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
			FeatureSymbol seg = _phoneticFeatSys.GetSymbol("seg");
			fs.AddValue(seg.Feature, new SymbolicFeatureValue(seg));
			table.AddSegmentDefinition(strRep, fs);
		}

		[Test]
		public void TestSimpleRules()
		{
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation(_table1.GetSegmentDefinition("t").FeatureStruct).Value;
			var rule1 = new PhonologicalRule("rule1", "rule1", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("asp+").Symbol("seg").Value).Value;
			var leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("seg").Value).Value;
			var rightEnv = new Expression<PhoneticShapeNode>();
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			lhs = Expression<PhoneticShapeNode>.With
				.Annotation(_table3.GetSegmentDefinition("p").FeatureStruct).Value;
			var rule2 = new PhonologicalRule("rule2", "rule2", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			rhs = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("asp+").Symbol("seg").Value).Value;
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("seg").Value).Value;
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			PhoneticShape shape = _table1.ToPhoneticShape("pHitH", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[p(pH)]i[t(tH)]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("pHit", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("pHitH", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("pit", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("pHitH", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("datH", ModeType.Analysis);
			Assert.IsFalse(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("da[t(tH)]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("dat", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("datH", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("gab", ModeType.Analysis);
			Assert.IsFalse(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("gab", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("gab", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gab", _table1.ToString(shape, ModeType.Synthesis, false));
		}

		[Test]
		public void TestLongDistanceRules()
		{
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("seg").Value).Value;
			var rule = new PhonologicalRule("rule", "rule", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("back+").Symbol("round+").Symbol("seg").Value).Value;
			var leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Symbol("seg").Value)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Value)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value).Value;
			var rightEnv = new Expression<PhoneticShapeNode>();
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			PhoneticShape shape = _table1.ToPhoneticShape("bubabu", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubab[iu(uû)ü]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubabu", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("bubabi", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, false));

			rule = new PhonologicalRule("rule", "rule", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			leftEnv = new Expression<PhoneticShapeNode>();
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Value)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Value)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Symbol("seg").Value).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("bubabu", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("b[iu(uû)ü]babu", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubabu", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("bübabu", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, false));
		}

		[Test]
		public void TestWordBoundaryRules()
		{
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Symbol("seg").Value).Value;
			var rule = new PhonologicalRule("rule", "rule", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("vd-").Symbol("asp-").Symbol("seg").Value).Value;
			var leftEnv = new Expression<PhoneticShapeNode>();
			var rightEnv = Expression<PhoneticShapeNode>.With.RightSideOfInput.Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			PhoneticShape shape = _table1.ToPhoneticShape("gap", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("ga[p(pH)b]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("ga'p", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gap", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table3.ToPhoneticShape("gab", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gap", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table3.ToPhoneticShape("ga+b", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gap", _table1.ToString(shape, ModeType.Synthesis, false));

			rule = new PhonologicalRule("rule", "rule", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			rightEnv = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Value)
				.RightSideOfInput.Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("kab", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[k(kH)g]ab", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("gab", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("kab", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table3.ToPhoneticShape("ga+b", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("kab", _table1.ToString(shape, ModeType.Synthesis, false));

			rule = new PhonologicalRule("rule", "rule", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.With.LeftSideOfInput.Value;
			rightEnv = new Expression<PhoneticShapeNode>();
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("kab", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("[k(kH)g]ab", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("gab", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("kab", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table3.ToPhoneticShape("ga+b", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("kab", _table1.ToString(shape, ModeType.Synthesis, false));

			rule = new PhonologicalRule("rule", "rule", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.With
				.LeftSideOfInput
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("voc-").Value)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Value).Value;
			rule.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("gap", ModeType.Analysis);
			Assert.IsTrue(rule.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("ga[p(pH)b]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("ga'p", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gap", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table3.ToPhoneticShape("gab", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gap", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table3.ToPhoneticShape("ga+b", ModeType.Synthesis);
			Assert.IsTrue(rule.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("gap", _table1.ToString(shape, ModeType.Synthesis, false));
		}

		[Test]
		public void TestQuantifierRules()
		{
			var lhs = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("seg").Value).Value;
			var rule1 = new PhonologicalRule("rule1", "rule1", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			var rhs = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("back+").Symbol("round+").Symbol("seg").Value).Value;
			var leftEnv = new Expression<PhoneticShapeNode>();
			var rightEnv = Expression<PhoneticShapeNode>.With
				.Group(g => g
	            	.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("seg").Value)
	            	.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Symbol("seg").Value)).Range(1, 2)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("seg").Value)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Symbol("seg").Value).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			var rule2 = new PhonologicalRule("rule2", "rule2", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("round+").Symbol("seg").Value)
				.Group(g => g
					.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("seg").Value)
					.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("low+").Symbol("seg").Value)).Range(1, 2)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons+").Symbol("seg").Value).Value;
			rightEnv = new Expression<PhoneticShapeNode>();
			rule2.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			PhoneticShape shape = _table1.ToPhoneticShape("bubu", ModeType.Analysis);
			Assert.IsFalse(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table3.ToPhoneticShape("b+ubu", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubu", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("bubabu", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("b[iu(uû)ü]bab[iu(uû)ü]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubabu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("bubabi", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("bübabu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabu", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("bibabi", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bibabi", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("bubababu", ModeType.Analysis);
			Assert.IsTrue(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("b[iu(uû)ü]babab[iu(uû)ü]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("bubababi", ModeType.Synthesis);
			Assert.IsFalse(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubababu", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("bibababu", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.IsTrue(rule2.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubababu", _table1.ToString(shape, ModeType.Synthesis, false));

			shape = _table1.ToPhoneticShape("bubabababu", ModeType.Analysis);
			Assert.IsFalse(rule2.AnalysisRule.Apply(shape.Annotations));
			Assert.IsFalse(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bubabababu", _table1.ToRegexString(shape, ModeType.Analysis, true));

			rule1 = new PhonologicalRule("rule1", "rule1", _spanFactory, 1, Direction.LeftToRight, false, lhs);
			leftEnv = Expression<PhoneticShapeNode>.With
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("back+").Symbol("round+").Symbol("seg").Value)
				.Annotation(FeatureStruct.With(_phoneticFeatSys).Symbol("cons-").Symbol("voc+").Symbol("high+").Symbol("seg").Value).LazyRange(0, 2).Value;
			rule1.AddSubrule(rhs, leftEnv, rightEnv, new FeatureStruct());

			shape = _table1.ToPhoneticShape("buuubuuu", ModeType.Analysis);
			Assert.IsTrue(rule1.AnalysisRule.Apply(shape.Annotations));
			Assert.AreEqual("bu[iu(uû)ü][iu(uû)ü]bu[iu(uû)ü][iu(uû)ü]", _table1.ToRegexString(shape, ModeType.Analysis, true));

			shape = _table1.ToPhoneticShape("buiibuii", ModeType.Synthesis);
			Assert.IsTrue(rule1.SynthesisRule.Apply(shape.Annotations));
			Assert.AreEqual("buuubuuu", _table1.ToString(shape, ModeType.Synthesis, false));
		}
	}
}
