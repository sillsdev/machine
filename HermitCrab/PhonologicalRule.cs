using SIL.APRE;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class should be extended by all phonological rules.
    /// </summary>
    public abstract class PhonologicalRule : IDBearer
    {
    	/// <summary>
        /// The multiple application order for phonological rules.
        /// </summary>
        public enum MultAppOrder { LeftToRightIterative, RightToLeftIterative, Simultaneous };

    	protected PhonologicalRule(string id, string desc)
            : base(id, desc)
    	{
    		TraceSynthesis = false;
    		TraceAnalysis = false;
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

    	/// <summary>
        /// Gets or sets the multiple application order.
        /// </summary>
        /// <value>The multiple application order.</value>
        public abstract MultAppOrder MultApplication
        {
            get;
            set;
        }

        /// <summary>
        /// Unapplies the rule to the specified word analysis.
        /// </summary>
        /// <param name="input">The input word analysis.</param>
        public abstract void Unapply(WordAnalysis input);

        /// <summary>
        /// Applies the rule to the specified word synthesis.
        /// </summary>
        /// <param name="input">The input word synthesis.</param>
        public abstract void Apply(WordSynthesis input);
    }
}
