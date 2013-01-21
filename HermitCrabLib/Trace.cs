using SIL.Collections;

namespace SIL.HermitCrab
{
	/// <summary>
	/// The type of trace record
	/// </summary>
	public enum TraceType
	{
		None,
		/// <summary>
		/// Word analysis trace
		/// </summary>
		WordAnalysis,
		/// <summary>
		/// Stratum synthesis input trace
		/// </summary>
		StratumSynthesisInput,
		/// <summary>
		/// Stratum synthesis output trace
		/// </summary>
		StratumSynthesisOutput,
		/// <summary>
		/// Stratum analysis input trace
		/// </summary>
		StratumAnalysisInput,
		/// <summary>
		/// Stratum analysis output trace
		/// </summary>
		StratumAnalysisOutput,
		/// <summary>
		/// Lexical lookup trace
		/// </summary>
		LexicalLookup,
		/// <summary>
		/// Blocking trace
		/// </summary>
		Blocking,
		/// <summary>
		/// Word synthesis trace
		/// </summary>
		WordSynthesis,
		/// <summary>
		/// Phonological rule analysis trace
		/// </summary>
		PhonologicalRuleAnalysis,
		/// <summary>
		/// Phonological rule synthesis trace
		/// </summary>
		PhonologicalRuleSynthesis,
		/// <summary>
		/// Affix template analysis input trace
		/// </summary>
		TemplateAnalysisInput,
		/// <summary>
		/// Affix template analysis output trace
		/// </summary>
		TemplateAnalysisOutput,
		/// <summary>
		/// Affix template synthesis input trace
		/// </summary>
		TemplateSynthesisInput,
		/// <summary>
		/// Affix template synthesis output trace
		/// </summary>
		TemplateSynthesisOutput,
		/// <summary>
		/// Morphological rule analysis trace
		/// </summary>
		MorphologicalRuleAnalysis,
		/// <summary>
		/// Morphological rule synthesis trace
		/// </summary>
		MorphologicalRuleSynthesis,
		/// <summary>
		/// Report success trace
		/// </summary>
		ReportSuccess
	}

    /// <summary>
    /// This class represents a trace record. All trace records inherit from this class.
    /// A morph trace is a tree structure where each node in the tree is a <c>Trace</c> object.
    /// </summary>
    public class Trace : OrderedBidirTreeNode<Trace>
    {
    	private readonly TraceType _type;
    	private readonly IHCRule _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="Trace"/> class.
        /// </summary>
        internal Trace(TraceType type, IHCRule source)
			: base(begin => new Trace(TraceType.None, null))
        {
        	_type = type;
        	_source = source;
        }

    	public TraceType Type
    	{
    		get { return _type; }
    	}

    	public IHCRule Source
    	{
    		get { return _source; }
    	}

		public Word Input { get; internal set; }

		public Word Output { get; internal set; }
    }
}