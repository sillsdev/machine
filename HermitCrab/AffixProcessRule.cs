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
    	private readonly AffixProcessAnalysisRule _analysisRule;
    	private readonly AffixProcessSynthesisRule _synthesisRule;

    	public AffixProcessRule(string id, SpanFactory<ShapeNode> spanFactory)
			: base(id)
    	{
			_analysisRule = new AffixProcessAnalysisRule(spanFactory, this);
			_synthesisRule = new AffixProcessSynthesisRule(spanFactory, this);

    		MaxApplicationCount = 1;
    		RequiredSyntacticFeatureStruct = new FeatureStruct();
    		OutSyntacticFeatureStruct = new FeatureStruct();
    	}

		public int MaxApplicationCount { get; set; }

		public FeatureStruct RequiredSyntacticFeatureStruct { get; set; }

		public FeatureStruct OutSyntacticFeatureStruct { get; set; }

		public override IRule<Word, ShapeNode> AnalysisRule
		{
			get { return _analysisRule; }
		}

		public override IRule<Word, ShapeNode> SynthesisRule
    	{
			get { return _synthesisRule; }
    	}

		public void AddAllomorph(AffixProcessAllomorph allomorph)
		{
			allomorph.Morpheme = this;
			_analysisRule.AddAllomorph(allomorph);
			_synthesisRule.AddAllomorph(allomorph);
		}
    }
}