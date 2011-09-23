using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	internal class SynthesisStandardPhonologicalRule : PatternRuleBatchBase<PhoneticShapeNode>
	{
		private readonly Expression<PhoneticShapeNode> _lhs;

		public SynthesisStandardPhonologicalRule(SpanFactory<PhoneticShapeNode> spanFactory, Direction dir, bool simult, Expression<PhoneticShapeNode> lhs)
			: base(new Pattern<PhoneticShapeNode>(spanFactory, dir), simult)
		{
			_lhs = lhs;
		}

		public void AddSubrule(Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv,
			FeatureStruct applicableFS)
		{
			if (_lhs.Children.Count == rhs.Children.Count)
				AddRuleInternal(new FeatureSynthesisRewriteRule(Lhs.SpanFactory, Lhs.Direction, Simultaneous, _lhs, rhs, leftEnv,
					rightEnv, applicableFS));
			else if (_lhs.Children.Count > rhs.Children.Count)
				AddRuleInternal(new NarrowSynthesisRewriteRule(Lhs.SpanFactory, Lhs.Direction, Simultaneous, _lhs, rhs, leftEnv,
					rightEnv, applicableFS));
			else if (_lhs.Children.Count == 0)
				AddRuleInternal(new EpenthesisSynthesisRewriteRule(Lhs.SpanFactory, Lhs.Direction, Simultaneous, _lhs, rhs, leftEnv,
					rightEnv, applicableFS));
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
					ann.FeatureStruct.RemoveValue(HCFeatureSystem.Backtrack);
				return true;
			}
			return false;
		}
	}
}
