using System.Collections.Generic;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	internal class StandardPhonologicalSynthesisRule : PatternRuleBatchBase<Word, ShapeNode>
	{
		private readonly Expression<Word, ShapeNode> _lhs;

		public StandardPhonologicalSynthesisRule(SpanFactory<ShapeNode> spanFactory, Direction dir, ApplicationMode appMode, Expression<Word, ShapeNode> lhs)
			: base(new Pattern<Word, ShapeNode>(spanFactory, dir), appMode)
		{
			_lhs = lhs;
		}

		public void AddSubrule(Expression<Word, ShapeNode> rhs, Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv,
			FeatureStruct applicableFS)
		{
			if (_lhs.Children.Count == rhs.Children.Count)
				AddRuleInternal(new FeatureSynthesisRewriteRule(Lhs.SpanFactory, Lhs.Direction, ApplicationMode, _lhs, rhs, leftEnv,
					rightEnv, applicableFS), true);
			else if (_lhs.Children.Count > rhs.Children.Count)
				AddRuleInternal(new NarrowSynthesisRewriteRule(Lhs.SpanFactory, Lhs.Direction, ApplicationMode, _lhs, rhs, leftEnv,
					rightEnv, applicableFS), true);
			else if (_lhs.Children.Count == 0)
				AddRuleInternal(new EpenthesisSynthesisRewriteRule(Lhs.SpanFactory, Lhs.Direction, ApplicationMode, _lhs, rhs, leftEnv,
					rightEnv, applicableFS), true);
		}

		public override bool IsApplicable(Word input)
		{
			return true;
		}

		public override bool Apply(Word input, out IEnumerable<Word> output)
		{
			if (base.Apply(input, out output))
			{
				foreach (Annotation<ShapeNode> ann in input.Annotations)
					ann.FeatureStruct.RemoveValue(HCFeatureSystem.Backtrack);
				return true;
			}
			return false;
		}
	}
}
