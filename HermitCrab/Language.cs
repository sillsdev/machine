using System;
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

		public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new LanguageAnalysisRule(spanFactory, morpher, this);
		}

		public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new LanguageSynthesisRule(spanFactory, morpher, this);
		}

    	public void Traverse(Action<IHCRule> action)
    	{
    		action(this);
			foreach (Stratum stratum in _strata)
				stratum.Traverse(action);
    	}
    }
}
