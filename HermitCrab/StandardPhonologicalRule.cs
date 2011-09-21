using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class should be extended by all phonological rules.
    /// </summary>
    public class StandardPhonologicalRule : IDBearerBase
    {
    	private readonly AnalysisStandardPhonologicalRule _analysisRule;
    	private readonly SynthesisStandardPhonologicalRule _synthesisRule;
    	private readonly Expression<PhoneticShapeNode> _lhs;
    	private readonly SpanFactory<PhoneticShapeNode> _spanFactory;
    	private readonly int _delReapplications;
    	private readonly Direction _dir;
    	private readonly bool _simult;

    	public StandardPhonologicalRule(string id, string description, SpanFactory<PhoneticShapeNode> spanFactory, int delReapplications,
			Direction dir, bool simult, Expression<PhoneticShapeNode> lhs)
			: base(id, description)
		{
			_spanFactory = spanFactory;
			_lhs = lhs;
			_dir = dir;
			_simult = simult;
			_delReapplications = delReapplications;
			_analysisRule = new AnalysisStandardPhonologicalRule(spanFactory, delReapplications, dir, simult, lhs);
			_synthesisRule = new SynthesisStandardPhonologicalRule(spanFactory, dir, simult, lhs);
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

		public void Compile()
		{
			_analysisRule.Compile();
			_synthesisRule.Compile();
		}

		public void AddSubrule(Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv,
			Expression<PhoneticShapeNode> rightEnv, FeatureStruct applicableFS)
		{
			_analysisRule.AddSubrule(rhs, leftEnv, rightEnv);
			_synthesisRule.AddSubrule(rhs, leftEnv, rightEnv, applicableFS);
		}
    }
}
