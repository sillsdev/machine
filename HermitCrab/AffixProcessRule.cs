using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents an affixal morphological rule. It supports many different types of affixation,
    /// such as prefixation, suffixation, infixation, circumfixation, simulfixation, reduplication,
    /// and truncation.
    /// </summary>
    public class AffixProcessRule : MorphologicalRule
    {
    	private readonly SpanFactory<ShapeNode> _spanFactory; 
    	private readonly List<AffixProcessAllomorph> _allomorphs; 

    	private AnalysisAffixProcessRule _analysisRule;
    	private SynthesisAffixProcessRule _synthesisRule;

    	public AffixProcessRule(string id, SpanFactory<ShapeNode> spanFactory)
			: base(id)
    	{
    		_spanFactory = spanFactory;
			_allomorphs = new List<AffixProcessAllomorph>();

    		MaxApplicationCount = 1;
    		RequiredSyntacticFeatureStruct = new FeatureStruct();
    		OutSyntacticFeatureStruct = new FeatureStruct();
    	}

		public int MaxApplicationCount { get; set; }

		public FeatureStruct RequiredSyntacticFeatureStruct { get; set; }

		public FeatureStruct OutSyntacticFeatureStruct { get; set; }

    	public IEnumerable<AffixProcessAllomorph> Allomorphs
    	{
    		get { return _allomorphs; }
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
			_analysisRule = new AnalysisAffixProcessRule(_spanFactory, this);
			_synthesisRule = new SynthesisAffixProcessRule(_spanFactory, this);
    	}

    	public void AddAllomorph(AffixProcessAllomorph allomorph)
		{
			allomorph.Morpheme = this;
			_allomorphs.Add(allomorph);
		}
    }
}