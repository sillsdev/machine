using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
    /// <summary>
    /// 
    /// </summary>
    public class StandardPhonologicalRule : PhonologicalRule
    {
    	private readonly List<AnalysisRewriteRuleSpec> _analysisRuleSpecs;
    	private AnalysisRewriteRule _analysisRule;
    	private readonly List<SynthesisRewriteRuleSpec> _synthesisRuleSpecs; 
    	private SynthesisRewriteRule _synthesisRule;
		private readonly Pattern<Word, ShapeNode> _lhs;
    	private readonly SpanFactory<ShapeNode> _spanFactory; 

    	public StandardPhonologicalRule(string id, SpanFactory<ShapeNode> spanFactory, Pattern<Word, ShapeNode> lhs)
			: base(id)
    	{
			ApplicationMode = ApplicationMode.Iterative;
    		_lhs = lhs;
    		_spanFactory = spanFactory;
			_analysisRuleSpecs = new List<AnalysisRewriteRuleSpec>();
			_synthesisRuleSpecs = new List<SynthesisRewriteRuleSpec>();
    	}

		public Direction Direction { get; set; }

		public ApplicationMode ApplicationMode { get; set; }

		public int DelReapplications { get; set; }

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
			_synthesisRule = new SynthesisRewriteRule(_spanFactory, _synthesisRuleSpecs, ApplicationMode, Direction);
			_analysisRule = new AnalysisRewriteRule(_spanFactory, _analysisRuleSpecs, ApplicationMode, Direction, DelReapplications);
		}

		public void AddSubrule(Pattern<Word, ShapeNode> rhs, Pattern<Word, ShapeNode> leftEnv,
			Pattern<Word, ShapeNode> rightEnv, FeatureStruct requiredSyntacticFS)
		{
			if (_lhs.Children.Count == rhs.Children.Count)
			{
				_synthesisRuleSpecs.Add(new FeatureSynthesisRewriteRuleSpec(_lhs, rhs, leftEnv, rightEnv, requiredSyntacticFS));
				_analysisRuleSpecs.Add(new FeatureAnalysisRewriteRuleSpec(_lhs, rhs, leftEnv, rightEnv));
			}
			else if (_lhs.Children.Count > rhs.Children.Count)
			{
				_synthesisRuleSpecs.Add(new NarrowSynthesisRewriteRuleSpec(_lhs, rhs, leftEnv, rightEnv, requiredSyntacticFS));
				_analysisRuleSpecs.Add(new NarrowAnalysisRewriteRuleSpec(_lhs, rhs, leftEnv, rightEnv));
			}
			else if (_lhs.Children.Count == 0)
			{
				_synthesisRuleSpecs.Add(new EpenthesisSynthesisRewriteRuleSpec(_lhs, rhs, leftEnv, rightEnv, requiredSyntacticFS));
				_analysisRuleSpecs.Add(new EpenthesisAnalysisRewriteRuleSpec(rhs, leftEnv, rightEnv));
			}
		}
    }
}
