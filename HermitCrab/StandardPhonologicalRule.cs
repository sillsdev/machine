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

    	public StandardPhonologicalRule(string id, string description, SpanFactory<ShapeNode> spanFactory, int delReapplications,
			Direction dir, ApplicationMode appMode, Expression<Word, ShapeNode> lhs)
			: base(id, description)
		{
			_analysisRule = new StandardPhonologicalAnalysisRule(spanFactory, delReapplications, dir, appMode, lhs);
			_synthesisRule = new StandardPhonologicalSynthesisRule(spanFactory, dir, appMode, lhs);
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
