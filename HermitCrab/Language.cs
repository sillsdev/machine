using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class acts as the main interface to the morphing capability of HC.NET. It encapsulates
    /// the feature systems, rules, character definition tables, etc. for a particular language.
    /// </summary>
    public class Language : IDBearerBase, IHCRule
    {
        private readonly List<Stratum> _strata;

    	private bool _traceStrataAnalysis;
        private bool _traceStrataSynthesis;
        private bool _traceTemplatesAnalysis;
        private bool _traceTemplatesSynthesis;
        private bool _traceLexLookup;
        private bool _traceBlocking;
        private bool _traceSuccess;

    	/// <summary>
    	/// Initializes a new instance of the <see cref="Language"/> class.
    	/// </summary>
    	/// <param name="id">The id.</param>
    	public Language(string id)
            : base(id)
        {
            _strata = new List<Stratum>();
			PhoneticFeatureSystem = new FeatureSystem();
			SyntacticFeatureSystem = new FeatureSystem();
        }

        /// <summary>
        /// Gets the surface stratum.
        /// </summary>
        /// <value>The surface stratum.</value>
        public Stratum SurfaceStratum
        {
            get
            {
				if (_strata.Count == 0)
					return null;
            	return _strata[_strata.Count - 1];
            }
        }

    	/// <summary>
    	/// Gets the phonetic feature system.
    	/// </summary>
    	/// <value>The phonetic feature system.</value>
		public FeatureSystem PhoneticFeatureSystem { get; set; }

    	/// <summary>
    	/// Gets the syntactic feature system.
    	/// </summary>
    	/// <value>The syntactic feature system.</value>
		public FeatureSystem SyntacticFeatureSystem { get; set; }

        /// <summary>
        /// Gets all strata, including the surface stratum.
        /// </summary>
        /// <value>The strata.</value>
        public IList<Stratum> Strata
        {
            get { return _strata; }
        }

    	/// <summary>
    	/// Gets or sets the maximum number of times a deletion phonological rule can be reapplied.
    	/// Default: 0.
    	/// </summary>
    	/// <value>Maximum number of delete reapplications.</value>
    	public int DelReapplications { get; set; }

        /// <summary>
        /// Gets a value indicating whether this morpher is tracing.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this morpher is tracing, otherwise <c>false</c>.
        /// </value>
        public bool IsTracing
        {
            get
            {
                if (_traceStrataAnalysis || _traceStrataSynthesis || _traceTemplatesAnalysis || _traceTemplatesSynthesis
                    || _traceLexLookup || _traceBlocking || _traceSuccess)
                {
                    return true;
                }

            	return false;
            }
        }

        /// <summary>
        /// Turns tracing on and off for all parts of the morpher.
        /// </summary>
        /// <value><c>true</c> to turn tracing on, <c>false</c> to turn tracing off.</value>
        public bool TraceAll
        {
            set
            {
                _traceStrataAnalysis = value;
                _traceStrataSynthesis = value;
                _traceTemplatesAnalysis = value;
                _traceTemplatesSynthesis = value;
                _traceLexLookup = value;
                _traceBlocking = value;
                _traceSuccess = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tracing of lexical lookup is
        /// on or off.
        /// </summary>
        /// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
        public bool TraceLexLookup
        {
            get
            {
                return _traceLexLookup;
            }

            set
            {
                _traceLexLookup = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tracing of blocking is
        /// on or off.
        /// </summary>
        /// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
        public bool TraceBlocking
        {
            get
            {
                return _traceBlocking;
            }

            set
            {
                _traceBlocking = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tracing of successful parses is
        /// on or off.
        /// </summary>
        /// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
        public bool TraceSuccess
        {
            get
            {
                return _traceSuccess;
            }

            set
            {
                _traceSuccess = value;
            }
        }

		public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new LanguageAnalysisRule(spanFactory, morpher, this);
		}

		public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new LanguageSynthesisRule(spanFactory, morpher, this);
		}
    }
}
