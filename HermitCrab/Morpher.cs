using System.Collections.Generic;
using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class acts as the main interface to the morphing capability of HC.NET. It encapsulates
    /// the feature systems, rules, character definition tables, etc. for a particular language.
    /// </summary>
    public class Morpher : IDBearerBase
    {
        private readonly FeatureSystem _phoneticFeatSys;
        private readonly FeatureSystem _syntacticFeatSys;
        private readonly List<Stratum> _orderedStrata;
    	private readonly IDBearerSet<Stratum> _strata; 
    	private readonly IDBearerSet<NaturalClass> _natClasses;
        private readonly IDBearerSet<StandardPhonologicalRule> _prules;
        private readonly IDBearerSet<MorphologicalRule> _mrules;
        private readonly IDBearerSet<AffixTemplate> _templates;
        private readonly Lexicon _lexicon;
        private readonly IDBearerSet<MprFeatureGroup> _mprFeatGroups;
        private readonly IDBearerSet<MprFeature> _mprFeatures;

    	private readonly MorpherAnalysisRule _analysisRule;

    	private bool _traceStrataAnalysis;
        private bool _traceStrataSynthesis;
        private bool _traceTemplatesAnalysis;
        private bool _traceTemplatesSynthesis;
        private bool _traceLexLookup;
        private bool _traceBlocking;
        private bool _traceSuccess;

    	/// <summary>
    	/// Initializes a new instance of the <see cref="Morpher"/> class.
    	/// </summary>
    	/// <param name="id">The id.</param>
    	/// <param name="phoneticFeatSys"></param>
    	/// <param name="syntacticFeatSys"></param>
    	/// <param name="lexicon"></param>
    	public Morpher(string id, FeatureSystem phoneticFeatSys, FeatureSystem syntacticFeatSys, Lexicon lexicon)
            : base(id)
        {
            _orderedStrata = new List<Stratum>();
			_strata = new IDBearerSet<Stratum>();
    		_phoneticFeatSys = phoneticFeatSys;
    		_syntacticFeatSys = syntacticFeatSys;
            _natClasses = new IDBearerSet<NaturalClass>();
            _prules = new IDBearerSet<StandardPhonologicalRule>();
            _mrules = new IDBearerSet<MorphologicalRule>();
    		_lexicon = lexicon;
            _templates = new IDBearerSet<AffixTemplate>();
            _mprFeatGroups = new IDBearerSet<MprFeatureGroup>();
            _mprFeatures = new IDBearerSet<MprFeature>();

    		_analysisRule = new MorpherAnalysisRule(this);
        }

        /// <summary>
        /// Gets the deepest stratum that is not the surface stratum.
        /// </summary>
        /// <value>The deepest stratum.</value>
        public Stratum DeepestStratum
        {
            get
            {
                if (_orderedStrata.Count < 2)
                    return null;
                return _orderedStrata[0];
            }
        }

        /// <summary>
        /// Gets the shallowest stratum that is not the surface stratum.
        /// </summary>
        /// <value>The shallowest stratum.</value>
        public Stratum ShallowestStratum
        {
            get
            {
                if (_orderedStrata.Count < 2)
                    return null;
                return _orderedStrata[_orderedStrata.Count - 2];
            }
        }

        /// <summary>
        /// Gets the surface stratum.
        /// </summary>
        /// <value>The surface stratum.</value>
        public Stratum SurfaceStratum
        {
            get
            {
				if (_orderedStrata.Count == 0)
					return null;
            	return _orderedStrata[_orderedStrata.Count - 1];
            }
        }

        /// <summary>
        /// Gets the phonetic feature system.
        /// </summary>
        /// <value>The phonetic feature system.</value>
        public FeatureSystem PhoneticFeatureSystem
        {
            get
            {
                return _phoneticFeatSys;
            }
        }

        /// <summary>
        /// Gets the syntactic feature system.
        /// </summary>
        /// <value>The syntactic feature system.</value>
        public FeatureSystem SyntacticFeatureSystem
        {
            get
            {
                return _syntacticFeatSys;
            }
        }

        /// <summary>
        /// Gets all strata, including the surface stratum.
        /// </summary>
        /// <value>The strata.</value>
        public IEnumerable<Stratum> Strata
        {
            get
            {
                return _orderedStrata;
            }
        }

        /// <summary>
        /// Gets the lexicon
        /// </summary>
        /// <value>The lexicon.</value>
        public Lexicon Lexicon
        {
            get
            {
                return _lexicon;
            }
        }

    	/// <summary>
    	/// Gets or sets the maximum number of times a deletion phonological rule can be reapplied.
    	/// Default: 0.
    	/// </summary>
    	/// <value>Maximum number of delete reapplications.</value>
    	public int DelReapplications { get; set; }

    	/// <summary>
        /// Gets the MPR feature groups.
        /// </summary>
        /// <value>The MPR feature groups.</value>
        public IEnumerable<MprFeatureGroup> MprFeatureGroups
        {
            get
            {
                return _mprFeatGroups;
            }
        }

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

                if (_prules.Any(prule => prule.TraceAnalysis || prule.TraceSynthesis))
                	return true;

            	return _mrules.Any(mrule => mrule.TraceAnalysis || mrule.TraceSynthesis);
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
                SetTraceRules(value, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tracing of strata during analysis is
        /// on or off.
        /// </summary>
        /// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
        public bool TraceStrataAnalysis
        {
            get
            {
                return _traceStrataAnalysis;
            }

            set
            {
                _traceStrataAnalysis = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tracing of strata during synthesis is
        /// on or off.
        /// </summary>
        /// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
        public bool TraceStrataSynthesis
        {
            get
            {
                return _traceStrataSynthesis;
            }

            set
            {
                _traceStrataSynthesis = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tracing of affix templates during analysis
        /// is on or off.
        /// </summary>
        /// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
        public bool TraceTemplatesAnalysis
        {
            get
            {
                return _traceTemplatesAnalysis;
            }

            set
            {
                _traceTemplatesAnalysis = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tracing of affix templates during synthesis
        /// is on or off.
        /// </summary>
        /// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
        public bool TraceTemplatesSynthesis
        {
            get
            {
                return _traceTemplatesSynthesis;
            }

            set
            {
                _traceTemplatesSynthesis = value;
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

        /// <summary>
        /// Turns tracing of all rules on or off.
        /// </summary>
        /// <param name="traceAnalysis"><c>true</c> if tracing during analysis is on, <c>false</c>
        /// if tracing during analysis is off.</param>
        /// <param name="traceSynthesis"><c>true</c> if tracing during synthesis is on, <c>false</c>
        /// if tracing during synthesis is off.</param>
        public void SetTraceRules(bool traceAnalysis, bool traceSynthesis)
        {
            foreach (StandardPhonologicalRule prule in _prules)
            {
                prule.TraceAnalysis = traceAnalysis;
                prule.TraceSynthesis = traceSynthesis;
            }

            foreach (MorphologicalRule mrule in _mrules)
            {
                mrule.TraceAnalysis = traceAnalysis;
                mrule.TraceSynthesis = traceSynthesis;
            }
        }

        /// <summary>
        /// Turns tracing of a rule on or off.
        /// </summary>
        /// <param name="id">The rule ID.</param>
        /// <param name="traceAnalysis"><c>true</c> if tracing during analysis is on, <c>false</c>
        /// if tracing during analysis is off.</param>
        /// <param name="traceSynthesis"><c>true</c> if tracing during synthesis is on, <c>false</c>
        /// if tracing during synthesis is off.</param>
        public void SetTraceRule(string id, bool traceAnalysis, bool traceSynthesis)
        {
            StandardPhonologicalRule prule = GetPhonologicalRule(id);
            if (prule != null)
            {
                prule.TraceAnalysis = traceAnalysis;
                prule.TraceSynthesis = traceSynthesis;
            }
            else
            {
                MorphologicalRule mrule = GetMorphologicalRule(id);
                if (mrule != null)
                {
                    mrule.TraceAnalysis = traceAnalysis;
                    mrule.TraceSynthesis = traceSynthesis;
                }
            }
        }

        /// <summary>
        /// Gets the stratum associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The stratum.</returns>
        public Stratum GetStratum(string id)
        {
            Stratum stratum;
            if (_strata.TryGetValue(id, out stratum))
                return stratum;
            return null;
        }

        /// <summary>
        /// Gets the natural class associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The natural class.</returns>
        public NaturalClass GetNaturalClass(string id)
        {
            NaturalClass nc;
            if (_natClasses.TryGetValue(id, out nc))
                return nc;
            return null;
        }

        /// <summary>
        /// Gets the phonological rule associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The phonological rule.</returns>
        public StandardPhonologicalRule GetPhonologicalRule(string id)
        {
            StandardPhonologicalRule prule;
            if (_prules.TryGetValue(id, out prule))
                return prule;
            return null;
        }

        /// <summary>
        /// Gets the morphological rule associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The morphological rule.</returns>
        public MorphologicalRule GetMorphologicalRule(string id)
        {
            MorphologicalRule mrule;
            if (_mrules.TryGetValue(id, out mrule))
                return mrule;
            return null;
        }

        /// <summary>
        /// Gets the affix template associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The affix template.</returns>
        public AffixTemplate GetAffixTemplate(string id)
        {
            AffixTemplate template;
            if (_templates.TryGetValue(id, out template))
                return template;
            return null;
        }

        /// <summary>
        /// Gets the MPR feature group associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The MPR feature group.</returns>
        public MprFeatureGroup GetMprFeatureGroup(string id)
        {
            MprFeatureGroup group;
            if (_mprFeatGroups.TryGetValue(id, out group))
                return group;
            return null;
        }

        /// <summary>
        /// Gets the MPR feature associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The MPR feature.</returns>
        public MprFeature GetMprFeature(string id)
        {
            MprFeature mprFeat;
            if (_mprFeatures.TryGetValue(id, out mprFeat))
                return mprFeat;
            return null;
        }

        /// <summary>
        /// Adds the stratum.
        /// </summary>
        /// <param name="stratum">The stratum.</param>
        public void AddStratum(Stratum stratum)
        {
            _orderedStrata.Add(stratum);
        	_strata.Add(stratum);
        }

        /// <summary>
        /// Adds the natural class.
        /// </summary>
        /// <param name="nc">The natural class.</param>
        public void AddNaturalClass(NaturalClass nc)
        {
            _natClasses.Add(nc);
        }

        /// <summary>
        /// Adds the phonological rule.
        /// </summary>
        /// <param name="prule">The phonological rule.</param>
        public void AddPhonologicalRule(StandardPhonologicalRule prule)
        {
            _prules.Add(prule);
        }

        /// <summary>
        /// Adds the morphological rule.
        /// </summary>
        /// <param name="mrule">The morphological rule.</param>
        public void AddMorphologicalRule(MorphologicalRule mrule)
        {
            _mrules.Add(mrule);
        }

        /// <summary>
        /// Adds the affix template.
        /// </summary>
        /// <param name="template">The affix template.</param>
        public void AddAffixTemplate(AffixTemplate template)
        {
            _templates.Add(template);
        }

        /// <summary>
        /// Adds the MPR feature group.
        /// </summary>
        /// <param name="group">The group.</param>
        public void AddMprFeatureGroup(MprFeatureGroup group)
        {
            _mprFeatGroups.Add(group);
        }

        /// <summary>
        /// Adds the MPR feature.
        /// </summary>
        /// <param name="mprFeature">The MPR feature.</param>
        public void AddMprFeature(MprFeature mprFeature)
        {
            _mprFeatures.Add(mprFeature);
        }

        /// <summary>
        /// Removes the natural class associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemoveNaturalClass(string id)
        {
            _natClasses.Remove(id);
        }

        /// <summary>
        /// Removes the phonological rule associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemovePhonologicalRule(string id)
        {
            _prules.Remove(id);
        }

        /// <summary>
        /// Removes the morphological rule associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemoveMorphologicalRule(string id)
        {
            _mrules.Remove(id);
        }

        /// <summary>
        /// Removes the affix template associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemoveAffixTemplate(string id)
        {
            _templates.Remove(id);
        }

        /// <summary>
        /// Removes the MPR feature group associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemoveMprFeatureGroup(string id)
        {
            _mprFeatGroups.Remove(id);
        }

        /// <summary>
        /// Removes the MPR feature associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemoveMprFeature(string id)
        {
            _mprFeatures.Remove(id);
        }

        /// <summary>
        /// Clears the strata.
        /// </summary>
        public void ClearStrata()
        {
            _orderedStrata.Clear();
			_strata.Clear();
        }

        /// <summary>
        /// Morphs the specified word.
        /// </summary>
        /// <param name="word">The word.</param>
        /// <returns>All valid word synthesis records.</returns>
        public ICollection<Word> MorphAndLookupWord(string word)
        {
            WordAnalysisTrace trace;
            return MorphAndLookupWord(word, out trace);
        }

        public ICollection<Word> MorphAndLookupWord(string word, out WordAnalysisTrace trace)
        {
            return MorphAndLookupToken(word, null, null, out trace);
        }

        /// <summary>
        /// Morphs the list of specified words.
        /// </summary>
        /// <param name="wordList">The word list.</param>
        /// <returns>All valid word synthesis records for each word.</returns>
        public IList<ICollection<Word>> MorphAndLookupWordList(IList<string> wordList)
        {
            IList<WordAnalysisTrace> traces;
            return MorphAndLookupWordList(wordList, out traces);
        }

        public IList<ICollection<Word>> MorphAndLookupWordList(IList<string> wordList,
            out IList<WordAnalysisTrace> traces)
        {
            var results = new List<ICollection<Word>>();
            traces = new List<WordAnalysisTrace>();
            string prev = null;
            string word = wordList[0];
            for (int i = 0; i < wordList.Count; i++)
            {
                string next = null;
                if (i + 1 < wordList.Count)
                    next = wordList[i + 1];

                WordAnalysisTrace trace;
                results.Add(MorphAndLookupToken(word, prev, next, out trace));
                traces.Add(trace);

                prev = word;
                word = next;
            }

            return results;
        }

		/// <summary>
		/// Does the real work of morphing the specified word.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <param name="prev">The previous word.</param>
		/// <param name="next">The next word.</param>
		/// <param name="trace">The trace.</param>
		/// <returns>All valid word synthesis records.</returns>
		private ICollection<Word> MorphAndLookupToken(string word, string prev, string next, out WordAnalysisTrace trace)
        {
            // convert the word to its phonetic shape
            var input = new Word(SurfaceStratum, word);

            // Unapply rules
			IEnumerable<Word> output;
			_analysisRule.Apply(input, out output);

			var candidates = new List<Word>();
			foreach (Word outWord in output)
			{
				IEnumerable<Word> lexLookupOutput;
				if (LexicalLookup(outWord, out lexLookupOutput))
					candidates.AddRange(lexLookupOutput);
			}

			trace = null;

			var results = new HashSet<Word>();
			return results;
        }

		private bool LexicalLookup(Word input, out IEnumerable<Word> output)
		{
			IEnumerable<LexEntry> entries;
			if (input.Stratum.SearchEntries(input.Shape, out entries))
			{
				var outputList = new List<Word>();
				foreach (LexEntry entry in entries)
				{
					foreach (RootAllomorph allomorph in entry.Allomorphs)
					{

					}
				}

				output = outputList;
				return true;
			}

			output = null;
			return false;
		}
    }
}
