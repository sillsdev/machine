using System;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
	public abstract class SynthesisRewriteSubruleSpec : RewriteSubruleSpec
	{
		private readonly RewriteSubrule _subrule;
		private readonly int _index;
		private readonly bool _isIterative;

		protected SynthesisRewriteSubruleSpec(SpanFactory<ShapeNode> spanFactory, MatcherSettings<ShapeNode> matcherSettings, bool isIterative,
			RewriteSubrule subrule, int index)
			: base(spanFactory, matcherSettings, subrule.LeftEnvironment, subrule.RightEnvironment)
		{
			_isIterative = isIterative;
			_subrule = subrule;
			_index = index;
		}

		protected bool IsIterative
		{
			get { return _isIterative; }
		}

		public override bool IsApplicable(Word input)
		{
			if (!_subrule.RequiredSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct))
			{
				if (input.CurrentRuleResults != null)
					input.CurrentRuleResults[_index] = new Tuple<FailureReason, object>(FailureReason.RequiredSyntacticFeatureStruct, _subrule.RequiredSyntacticFeatureStruct);
				return false;
			}

			MprFeatureGroup group;
			if (_subrule.RequiredMprFeatures.Count > 0 && !_subrule.RequiredMprFeatures.IsMatchRequired(input.MprFeatures, out group))
			{
				if (input.CurrentRuleResults != null)
					input.CurrentRuleResults[_index] = new Tuple<FailureReason, object>(FailureReason.RequiredMprFeatures, group);
				return false;
			}

			if (_subrule.ExcludedMprFeatures.Count > 0 && !_subrule.ExcludedMprFeatures.IsMatchExcluded(input.MprFeatures, out group))
			{
				if (input.CurrentRuleResults != null)
					input.CurrentRuleResults[_index] = new Tuple<FailureReason, object>(FailureReason.ExcludedMprFeatures, group);
				return false;
			}

			return true;
		}

		protected void MarkSuccessfulApply(Word word)
		{
			if (word.CurrentRuleResults != null)
				word.CurrentRuleResults[_index] = new Tuple<FailureReason, object>(FailureReason.None, null);
		}
	}
}
