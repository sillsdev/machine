using System.Linq;
using NUnit.Framework;
using SIL.APRE;
using SIL.HermitCrab;

namespace HermitCrabTest
{
	[TestFixture]
	public class StandardPhonologicalRuleTest
	{
		private SpanFactory<PhoneticShapeNode> _spanFactory;
		private FeatureSystem _phoneticFeatSys;
		private CharacterDefinitionTable _charDefTable;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			_spanFactory = new SpanFactory<PhoneticShapeNode>((x, y) => x.CompareTo(y), (start, end) => start.GetNodes(end).Count());
			_phoneticFeatSys = new FeatureSystem();
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("voc"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("cons"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("high"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("low"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("back"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("round"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("vd"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("asp"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("del_rel"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("ATR"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("strident"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("cont"));
			_phoneticFeatSys.AddFeature(CreatePhonologicalFeature("nasal"));
			var poaFeature = new SymbolicFeature("poa");
			poaFeature.AddPossibleSymbol(new FeatureSymbol("bilabial"));
			poaFeature.AddPossibleSymbol(new FeatureSymbol("labiodental"));
			poaFeature.AddPossibleSymbol(new FeatureSymbol("alveolar"));
			poaFeature.AddPossibleSymbol(new FeatureSymbol("velar"));
			_phoneticFeatSys.AddFeature(poaFeature);

			_charDefTable = new CharacterDefinitionTable("table1", "table1", _spanFactory, _phoneticFeatSys);
			_charDefTable.AddSegmentDefinition("a", _phoneticFeatSys.CreateFeatureStructure("cons_minus", "voc_plus", "high_minus", "low_plus", "back_plus", "round_minus", "vd_plus", "cont_plus"));
			_charDefTable.AddSegmentDefinition("i", _phoneticFeatSys.CreateFeatureStructure("cons_minus", "voc_plus", "high_plus", "low_minus", "back_minus", "round_minus", "vd_plus", "cont_plus"));
			_charDefTable.AddSegmentDefinition("u", _phoneticFeatSys.CreateFeatureStructure("cons_minus", "voc_plus", "high_minus", "low_minus", "back_plus", "round_plus", "vd_plus", "cont_plus"));
			_charDefTable.AddSegmentDefinition("o", _phoneticFeatSys.CreateFeatureStructure("cons_minus", "voc_plus", "high_minus", "low_minus", "back_plus", "round_plus", "vd_plus", "cont_plus"));
			_charDefTable.AddSegmentDefinition("uû", _phoneticFeatSys.CreateFeatureStructure("cons_minus", "voc_plus", "high_plus", "low_minus", "back_minus", "round_plus", "vd_plus", "cont_plus"));
			_charDefTable.AddSegmentDefinition("p", _phoneticFeatSys.CreateFeatureStructure("cons_plus", "voc_minus", "bilabial", "vd_minus", "asp_minus", "strident_minus", "cont_minus", "nasal_minus"));
			_charDefTable.AddSegmentDefinition("t", _phoneticFeatSys.CreateFeatureStructure("cons_plus", "voc_minus", "alveolar", "vd_minus", "asp_minus", "del_rel_minus", "strident_minus", "cont_minus", "nasal_minus"));
			_charDefTable.AddSegmentDefinition("pH", _phoneticFeatSys.CreateFeatureStructure("cons_plus", "voc_minus", "bilabial", "vd_minus", "asp_plus", "strident_minus", "cont_minus", "nasal_minus"));
			_charDefTable.AddSegmentDefinition("tH", _phoneticFeatSys.CreateFeatureStructure("cons_plus", "voc_minus", "alveolar", "vd_minus", "asp_plus", "del_rel_minus", "strident_minus", "cont_minus", "nasal_minus"));
		}

		private static Feature CreatePhonologicalFeature(string id)
		{
			var feature = new SymbolicFeature(id);
			feature.AddPossibleSymbol(new FeatureSymbol(id + "_plus", "+"));
			feature.AddPossibleSymbol(new FeatureSymbol(id + "_minus", "-"));
			return feature;
		}

		[Test]
		public void TestRule()
		{
			var rule1 = new StandardPhonologicalRule("rule1", "rule1", _spanFactory, _phoneticFeatSys);
			rule1.Lhs = new Pattern<PhoneticShapeNode>(_spanFactory, new AnnotationConstraints<PhoneticShapeNode>("Segment", (FeatureStructure) _charDefTable.GetSegmentDefinition("t").SynthFeatureStructure.Clone()));
			rule1.AddSubrule(new StandardPhonologicalRule.Subrule(1,
				new Pattern<PhoneticShapeNode>(_spanFactory, new AnnotationConstraints<PhoneticShapeNode>("Segment", _phoneticFeatSys.CreateFeatureStructure("asp_plus"))),
				new Pattern<PhoneticShapeNode>(_spanFactory, new AnnotationConstraints<PhoneticShapeNode>("Segment", _phoneticFeatSys.CreateFeatureStructure("cons_minus"))), null, rule1));
			rule1.Unapply(new WordAnalysis(_charDefTable.ToPhoneticShape("pHitH", ModeType.Analysis), null, null));
		}
	}
}
