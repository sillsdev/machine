using SIL.APRE;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	internal class AnalysisStandardPhonologicalRule : RuleCascadeBase<PhoneticShapeNode>
	{
		private readonly SpanFactory<PhoneticShapeNode> _spanFactory;
		private readonly int _delReapplications;
		private readonly Direction _synthesisDir;
		private readonly bool _synthesisSimult;
		private readonly Expression<PhoneticShapeNode> _lhs;

		public AnalysisStandardPhonologicalRule(SpanFactory<PhoneticShapeNode> spanFactory, int delReapplications, Direction synthesisDir,
			bool synthesisSimult, Expression<PhoneticShapeNode> lhs)
			: base(RuleOrder.Linear)
		{
			_spanFactory = spanFactory;
			_delReapplications = delReapplications;
			_synthesisDir = synthesisDir;
			_synthesisSimult = synthesisSimult;
			_lhs = lhs;
		}

		public void Compile()
		{
			foreach (AnalysisRewriteRule subrule in Rules)
				subrule.Compile();
		}

		public void AddSubrule(Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv)
		{
			if (_lhs.Children.Count == rhs.Children.Count)
				AddRuleInternal(new FeatureAnalysisRewriteRule(_spanFactory, _synthesisDir, _synthesisSimult, _lhs, rhs, leftEnv, rightEnv));
			else if (_lhs.Children.Count > rhs.Children.Count)
				AddRuleInternal(new NarrowAnalysisRewriteRule(_spanFactory, _lhs, rhs, leftEnv, rightEnv));
			else if (_lhs.Children.Count == 0)
				AddRuleInternal(new EpenthesisAnalysisRewriteRule(_spanFactory, _synthesisDir, _synthesisSimult, rhs, leftEnv, rightEnv));
		}

		public override bool IsApplicable(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			return true;
		}

		public override bool Apply(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			bool result = base.Apply(input);

			return result;
		}

		protected override bool ApplyRule(IRule<PhoneticShapeNode> rule, IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			bool applied = false;
			switch (((AnalysisRewriteRule) rule).AnalysisReapplyType)
			{
				case AnalysisReapplyType.Normal:
					if (base.ApplyRule(rule, input))
					{
						RemoveSearchedValue(input);
						applied = true;
					}
					break;

				case AnalysisReapplyType.Deletion:
					int i = 0;
					while (i <= _delReapplications && base.ApplyRule(rule, input))
					{
						RemoveSearchedValue(input);
						i++;
					}
					applied = i > 0;
					break;

				case AnalysisReapplyType.SelfOpaquing:
					while (base.ApplyRule(rule, input))
					{
						RemoveSearchedValue(input);
						applied = true;
					}
					break;
			}

			return applied;
		}

		private void RemoveSearchedValue(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			foreach (Annotation<PhoneticShapeNode> ann in input)
				ann.FeatureStruct.RemoveValue(HCFeatureSystem.Backtrack);
		}
	}
}
