using SIL.APRE;
using SIL.APRE.Transduction;

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
			_analysisRule = new AffixProcessAnalysisRule(spanFactory);
			_synthesisRule = new AffixProcessSynthesisRule(spanFactory);
    	}

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
			_analysisRule.AddAllomorph(allomorph);
			_synthesisRule.AddAllomorph(allomorph);
		}
    }
}