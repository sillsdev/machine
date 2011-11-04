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
    	private readonly AnalysisRewriteRuleCascade _analysisRule;
    	private readonly SynthesisRewriteRuleBatch _synthesisRule;
		private readonly Expression<Word, ShapeNode> _lhs;
    	private readonly SpanFactory<ShapeNode> _spanFactory; 

    	public StandardPhonologicalRule(string id, SpanFactory<ShapeNode> spanFactory, Expression<Word, ShapeNode> lhs)
			: base(id)
    	{
    		_lhs = lhs;
    		_spanFactory = spanFactory;
			_analysisRule = new AnalysisRewriteRuleCascade();
			_synthesisRule = new SynthesisRewriteRuleBatch(spanFactory);
    	}

    	public Direction Direction
    	{
			get { return _synthesisRule.Direction; }
			set
			{
				_synthesisRule.Direction = value;
				_analysisRule.SynthesisDirection = value;
			}
    	}

    	public ApplicationMode ApplicationMode
    	{
    		get { return _synthesisRule.ApplicationMode; }
    		set
    		{
    			_synthesisRule.ApplicationMode = value;
    			_analysisRule.SynthesisApplicationMode = value;
    		}
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
			_synthesisRule.Lhs.Compile();
		}

		public void AddSubrule(Expression<Word, ShapeNode> rhs, Expression<Word, ShapeNode> leftEnv,
			Expression<Word, ShapeNode> rightEnv, FeatureStruct applicableFS)
		{
			if (_lhs.Children.Count == rhs.Children.Count)
			{
				_synthesisRule.AddSubrule(new FeatureSynthesisRewriteRule(_spanFactory, _lhs, rhs, leftEnv, rightEnv, applicableFS));
				_analysisRule.AddSubrule(new FeatureAnalysisRewriteRule(_spanFactory, _lhs, rhs, leftEnv, rightEnv) {SynthesisApplicationMode = ApplicationMode, SynthesisDirection = Direction});
			}
			else if (_lhs.Children.Count > rhs.Children.Count)
			{
				_synthesisRule.AddSubrule(new NarrowSynthesisRewriteRule(_spanFactory, _lhs, rhs, leftEnv, rightEnv, applicableFS));
				_analysisRule.AddSubrule(new NarrowAnalysisRewriteRule(_spanFactory, _lhs, rhs, leftEnv, rightEnv) {SynthesisApplicationMode = ApplicationMode, SynthesisDirection = Direction});
			}
			else if (_lhs.Children.Count == 0)
			{
				_synthesisRule.AddSubrule(new EpenthesisSynthesisRewriteRule(_spanFactory, _lhs, rhs, leftEnv, rightEnv, applicableFS));
				_analysisRule.AddSubrule(new EpenthesisAnalysisRewriteRule(_spanFactory, rhs, leftEnv, rightEnv) {SynthesisApplicationMode = ApplicationMode, SynthesisDirection = Direction});
			}
		}
    }
}
