using System;
using SIL.APRE;
using SIL.APRE.FeatureModel;
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
			foreach (AnalysisStandardPhonologicalSubrule subrule in Rules)
				subrule.Compile();
		}

		public void AddSubrule(Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv)
		{
			AddRuleInternal(new AnalysisStandardPhonologicalSubrule(_spanFactory, _delReapplications, _synthesisDir, _synthesisSimult, _lhs,
				rhs, leftEnv, rightEnv));
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
	}
}
