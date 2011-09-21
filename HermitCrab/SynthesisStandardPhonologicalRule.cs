using System;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	internal class SynthesisStandardPhonologicalRule : PatternRuleBatchBase<PhoneticShapeNode>
	{
		private readonly Expression<PhoneticShapeNode> _lhs;

		private readonly StringFeature _searchedFeature;

		public SynthesisStandardPhonologicalRule(SpanFactory<PhoneticShapeNode> spanFactory, Direction dir, bool simult, Expression<PhoneticShapeNode> lhs)
			: base(new Pattern<PhoneticShapeNode>(spanFactory, dir), simult)
		{
			_lhs = lhs;

			_searchedFeature = new StringFeature(Guid.NewGuid().ToString(), "Searched");
		}

		public void AddSubrule(Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv,
			Expression<PhoneticShapeNode> rightEnv, FeatureStruct applicableFS)
		{
			AddRuleInternal(new SynthesisStandardPhonologicalSubrule(Lhs.SpanFactory, Lhs.Direction, Simultaneous, _lhs, rhs, leftEnv, rightEnv, applicableFS, _searchedFeature));
		}

		public override bool IsApplicable(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			return true;
		}

		public override bool Apply(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			if (base.Apply(input))
			{
				foreach (Annotation<PhoneticShapeNode> ann in input)
					ann.FeatureStruct.RemoveValue(_searchedFeature);
				return true;
			}
			return false;
		}
	}
}
