using System.Collections.Generic;
using System.Linq;
using SIL.APRE;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	internal class StandardPhonologicalAnalysisRule : RuleCascadeBase<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly Direction _synthesisDir;
		private readonly ApplicationMode _synthesisAppMode;
		private readonly Expression<Word, ShapeNode> _lhs;

		public StandardPhonologicalAnalysisRule(SpanFactory<ShapeNode> spanFactory, Direction synthesisDir,
			ApplicationMode synthesisAppMode, Expression<Word, ShapeNode> lhs)
			: base(RuleCascadeOrder.Linear)
		{
			_spanFactory = spanFactory;
			_synthesisDir = synthesisDir;
			_synthesisAppMode = synthesisAppMode;
			_lhs = lhs;
		}

		public int DelReapplications { get; set; }

		public void Compile()
		{
			foreach (AnalysisRewriteRule subrule in Rules)
				subrule.Compile();
		}

		public void AddSubrule(Expression<Word, ShapeNode> rhs, Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv)
		{
			if (_lhs.Children.Count == rhs.Children.Count)
				AddRuleInternal(new FeatureAnalysisRewriteRule(_spanFactory, _synthesisDir, _synthesisAppMode, _lhs, rhs, leftEnv, rightEnv), false);
			else if (_lhs.Children.Count > rhs.Children.Count)
				AddRuleInternal(new NarrowAnalysisRewriteRule(_spanFactory, _lhs, rhs, leftEnv, rightEnv), false);
			else if (_lhs.Children.Count == 0)
				AddRuleInternal(new EpenthesisAnalysisRewriteRule(_spanFactory, _synthesisDir, _synthesisAppMode, rhs, leftEnv, rightEnv), false);
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
