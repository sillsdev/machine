using System.Collections.Generic;
using System.Linq;
using SIL.APRE;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class AnalysisRewriteRuleCascade : RuleCascade<Word, ShapeNode>
	{
		private Direction _synthesisDir;
		private ApplicationMode _synthesisAppMode;

		public AnalysisRewriteRuleCascade()
		{
			_synthesisDir = Direction.RightToLeft;
			_synthesisAppMode = ApplicationMode.Iterative;
		}

		public int DelReapplications { get; set; }

		public Direction SynthesisDirection
		{
			get { return _synthesisDir; }
			set
			{
				_synthesisDir = value;
				foreach (AnalysisRewriteRule rule in Rules)
					rule.SynthesisDirection = value;
			}
		}

		public ApplicationMode SynthesisApplicationMode
		{
			get { return _synthesisAppMode; }
			set
			{
				_synthesisAppMode = value;
				foreach (AnalysisRewriteRule rule in Rules)
					rule.SynthesisApplicationMode = value;
			}
		}

		public void Compile()
		{
			foreach (AnalysisRewriteRule subrule in Rules)
				subrule.Lhs.Compile();
		}

		public void AddSubrule(AnalysisRewriteRule rule)
		{
			InsertRuleInternal(0, rule);
		}

		public override bool IsApplicable(Word input)
		{
			return true;
		}

		public override bool Apply(Word input, out IEnumerable<Word> output)
		{
			bool result = base.Apply(input, out output);

			return result;
		}

		protected override bool ApplyRule(IRule<Word, ShapeNode> rule, Word input, out IEnumerable<Word> output)
		{
			output = null;
			var rewriteRule = (AnalysisRewriteRule) rule;
			switch (rewriteRule.AnalysisReapplyType)
			{
				case AnalysisReapplyType.Normal:
					{
						IEnumerable<Word> result;
						if (base.ApplyRule(rewriteRule, input, out result))
						{
							if (rewriteRule.ApplicationMode == ApplicationMode.Iterative)
								RemoveSearchedValue(input);
							output = result;
						}
					}
					break;

				case AnalysisReapplyType.Deletion:
					{
						int i = 0;
						IEnumerable<Word> result;
						while (i <= DelReapplications && base.ApplyRule(rewriteRule, input, out result))
						{
							input = result.Single();
							output = input.ToEnumerable();
							i++;
						}
					}
					break;

				case AnalysisReapplyType.SelfOpaquing:
					{
						IEnumerable<Word> result;
						while (base.ApplyRule(rewriteRule, input, out result))
						{
							if (rewriteRule.ApplicationMode == ApplicationMode.Iterative)
								RemoveSearchedValue(input);
							input = result.Single();
							output = input.ToEnumerable();
						}
					}
					break;
			}

			return output != null;
		}

		private void RemoveSearchedValue(IData<ShapeNode> input)
		{
			foreach (Annotation<ShapeNode> ann in input.Annotations)
				ann.FeatureStruct.RemoveValue(HCFeatureSystem.Backtrack);
		}
	}
}
