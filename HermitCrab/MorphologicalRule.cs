using SIL.Machine;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class should be extended by all morphological rules.
    /// </summary>
    public abstract class MorphologicalRule : Morpheme
    {
    	protected MorphologicalRule(string id)
            : base(id)
        {
        }

    	/// <summary>
    	/// Gets or sets a value indicating whether tracing of this morphological rule
    	/// during analysis is on or off.
    	/// </summary>
    	/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
    	public bool TraceAnalysis { get; set; }

    	/// <summary>
    	/// Gets or sets a value indicating whether tracing of this morphological rule
    	/// during synthesis is on or off.
    	/// </summary>
    	/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
    	public bool TraceSynthesis { get; set; }

		public abstract IRule<Word, ShapeNode> AnalysisRule { get; }

		public abstract IRule<Word, ShapeNode> SynthesisRule { get; }
    }

}