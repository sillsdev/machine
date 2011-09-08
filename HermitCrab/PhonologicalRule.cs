using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class should be extended by all phonological rules.
    /// </summary>
    public class PhonologicalRule : IDBearerBase
    {
    	private readonly AnalysisPhonologicalRule _analysisRule;
    	private readonly SynthesisPhonologicalRule _synthesisRule;
    	private readonly Expression<PhoneticShapeNode> _lhs;
    	private readonly SpanFactory<PhoneticShapeNode> _spanFactory;
    	private readonly int _delReapplications;
    	private readonly Direction _dir;
    	private readonly bool _simult;

    	public PhonologicalRule(string id, string description, SpanFactory<PhoneticShapeNode> spanFactory, int delReapplications,
			Direction dir, bool simult, Expression<PhoneticShapeNode> lhs)
			: base(id, description)
		{
			TraceSynthesis = false;
			TraceAnalysis = false;
			_spanFactory = spanFactory;
			_lhs = lhs;
			_dir = dir;
			_simult = simult;
			_delReapplications = delReapplications;
			_analysisRule = new AnalysisPhonologicalRule(this);
			_synthesisRule = new SynthesisPhonologicalRule(this, spanFactory, dir, simult);
    	}

    	/// <summary>
    	/// Gets or sets a value indicating whether tracing of this phonological rule
    	/// during analysis is on or off.
    	/// </summary>
    	/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
    	public bool TraceAnalysis { get; set; }

    	/// <summary>
    	/// Gets or sets a value indicating whether tracing of this phonological rule
    	/// during synthesis is on or off.
    	/// </summary>
    	/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
    	public bool TraceSynthesis { get; set; }

    	public IRule<PhoneticShapeNode> AnalysisRule
    	{
    		get { return _analysisRule; }
    	}

    	public IRule<PhoneticShapeNode> SynthesisRule
    	{
    		get { return _synthesisRule; }
    	}

		public void AddSubrule(Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv,
			Expression<PhoneticShapeNode> rightEnv, FeatureStruct applicableFS)
		{
			_analysisRule.AddRule(new AnalysisPhonologicalSubrule(_spanFactory, _delReapplications, _dir, _simult, _lhs, rhs, leftEnv, rightEnv));
			_synthesisRule.AddRule(new SynthesisPhonologicalSubrule(_spanFactory, _dir, _simult, _lhs, rhs, leftEnv, rightEnv, applicableFS));
		}

		private class AnalysisPhonologicalRule : RuleCascade<PhoneticShapeNode>
		{
			private readonly PhonologicalRule _rule;

			public AnalysisPhonologicalRule(PhonologicalRule rule)
				: base(RuleOrder.Linear)
			{
				_rule = rule;
			}

			public override bool Apply(IBidirList<Annotation<PhoneticShapeNode>> input)
			{
				// TODO: do tracing here
				bool result = base.Apply(input);

				return result;
			}
		}

		private class SynthesisPhonologicalRule : PatternRuleBatch<PhoneticShapeNode>
		{
			private readonly PhonologicalRule _rule;

			public SynthesisPhonologicalRule(PhonologicalRule rule, SpanFactory<PhoneticShapeNode> spanFactory, Direction dir, bool simult)
				: base(new Pattern<PhoneticShapeNode>(spanFactory, dir), simult)
			{
				_rule = rule;
			}

			public override bool Apply(IBidirList<Annotation<PhoneticShapeNode>> input)
			{
				// TODO: do tracing here
				return base.Apply(input);
			}
		}
    }
}
