using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
    /// <summary>
    /// 
    /// </summary>
    public class StandardPhonologicalRule : PhonologicalRule
    {
    	private readonly StandardPhonologicalAnalysisRule _analysisRule;
    	private readonly StandardPhonologicalSynthesisRule _synthesisRule;

    	public StandardPhonologicalRule(string id, SpanFactory<ShapeNode> spanFactory, Expression<Word, ShapeNode> lhs)
			: base(id)
		{
			_analysisRule = new StandardPhonologicalAnalysisRule(spanFactory, dir, appMode, lhs);
			_synthesisRule = new StandardPhonologicalSynthesisRule(spanFactory, lhs);
    	}

    	public Direction Direction
    	{
			get { return _synthesisRule.Lhs.Direction; }
			set { _synthesisRule.Lhs.Direction = value; }
    	}

    	public ApplicationMode ApplicationMode
    	{
    		get { return _synthesisRule.ApplicationMode; }
    		set { _synthesisRule.ApplicationMode = value; }
    	}

    	public int DelReapplications
    	{
			get { return _analysisRule.DelReapplications; }
			set { _analysisRule.DelReapplications = value; }
    	}

		public override IRule<Word, ShapeNode> AnalysisRule
    	{
    		get { return _analysisRule; }
    	}

		public override IRule<Word, ShapeNode> SynthesisRule
    	{
    		get { return _synthesisRule; }
    	}

		public override void Compile()
		{
			_analysisRule.Compile();
			_synthesisRule.Compile();
		}

		public void AddSubrule(Expression<Word, ShapeNode> rhs, Expression<Word, ShapeNode> leftEnv,
			Expression<Word, ShapeNode> rightEnv, FeatureStruct applicableFS)
		{
			_analysisRule.AddSubrule(rhs, leftEnv, rightEnv);
			_synthesisRule.AddSubrule(rhs, leftEnv, rightEnv, applicableFS);
		}
    }
}
