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
        private readonly FeatureSystem _headFeatSys;
        private readonly FeatureSystem _footFeatSys;
        private readonly List<Stratum> _orderedStrata;
    	private readonly IDBearerSet<Stratum> _strata; 
        private readonly IDBearerSet<CharacterDefinitionTable> _charDefTables;
    	private readonly IDBearerSet<NaturalClass> _natClasses;
        private readonly IDBearerSet<StandardPhonologicalRule> _prules;
        private readonly IDBearerSet<MorphologicalRule> _mrules;
        private readonly IDBearerSet<AffixTemplate> _templates;
        private readonly Lexicon _lexicon;
        private readonly IDBearerSet<MprFeatureGroup> _mprFeatGroups;
        private readonly IDBearerSet<MprFeature> _mprFeatures;
        private readonly IDBearerSet<PartOfSpeech> _partsOfSpeech;
        private readonly IDBearerSet<Allomorph> _allomorphs;

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
		/// <param name="language">The language.</param>
        public Morpher(string id, string language)
            : base(id, language)
        {
            _orderedStrata = new List<Stratum>();
			_strata = new IDBearerSet<Stratum>();
            _phoneticFeatSys = new FeatureSystem();
            _headFeatSys = new FeatureSystem();
            _footFeatSys = new FeatureSystem();
            _charDefTables = new IDBearerSet<CharacterDefinitionTable>();
            _natClasses = new IDBearerSet<NaturalClass>();
            _prules = new IDBearerSet<StandardPhonologicalRule>();
            _mrules = new IDBearerSet<MorphologicalRule>();
            _lexicon = new Lexicon();
            _templates = new IDBearerSet<AffixTemplate>();
            _mprFeatGroups = new IDBearerSet<MprFeatureGroup>();
            _mprFeatures = new IDBearerSet<MprFeature>();
            _partsOfSpeech = new IDBearerSet<PartOfSpeech>();
            _allomorphs = new IDBearerSet<Allomorph>();
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
        /// Gets the head feature system.
        /// </summary>
        /// <value>The head feature system.</value>
        public FeatureSystem HeadFeatureSystem
        {
            get
            {
                return _headFeatSys;
            }
        }

        /// <summary>
        /// Gets the foot feature system.
        /// </summary>
        /// <value>The foot feature system.</value>
        public FeatureSystem FootFeatureSystem
        {
            get
            {
                return _footFeatSys;
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
        /// Gets the character definition table associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The character definition table.</returns>
        public CharacterDefinitionTable GetCharacterDefinitionTable(string id)
        {
            CharacterDefinitionTable charDefTable;
            if (_charDefTables.TryGetValue(id, out charDefTable))
                return charDefTable;
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
        /// Gets the part of speech associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The part of speech.</returns>
        public PartOfSpeech GetPartOfSpeech(string id)
        {
            PartOfSpeech pos;
            if (_partsOfSpeech.TryGetValue(id, out pos))
                return pos;
            return null;
        }

        /// <summary>
        /// Gets the morpheme associated with the specified ID. Morphological rules
        /// and lexical entries are morphemes.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The morpheme.</returns>
        public Morpheme GetMorpheme(string id)
        {
        	return GetMorphologicalRule(id) ?? (Morpheme) Lexicon.GetEntry(id);
        }

        /// <summary>
        /// Gets the allomorph associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The allomorph.</returns>
        public Allomorph GetAllomorph(string id)
        {
            Allomorph allomorph;
            if (_allomorphs.TryGetValue(id, out allomorph))
                return allomorph;
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
        /// Adds the character definition table.
        /// </summary>
        /// <param name="charDefTable">The character definition table.</param>
        public void AddCharacterDefinitionTable(CharacterDefinitionTable charDefTable)
        {
            _charDefTables.Add(charDefTable);
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
        /// Adds the part of speech.
        /// </summary>
        /// <param name="pos">The part of speech.</param>
        public void AddPartOfSpeech(PartOfSpeech pos)
        {
            _partsOfSpeech.Add(pos);
        }

        /// <summary>
        /// Adds the allomorph.
        /// </summary>
        /// <param name="allomorph">The allomorph.</param>
        public void AddAllomorph(Allomorph allomorph)
        {
            _allomorphs.Add(allomorph);
        }

        /// <summary>
        /// Removes the character definition table associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemoveCharacterDefinitionTable(string id)
        {
            _charDefTables.Remove(id);
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
        /// Removes the part of speech associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemovePartOfSpeech(string id)
        {
            _partsOfSpeech.Remove(id);
        }

        /// <summary>
        /// Removes the allomorph associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemoveAllomorph(string id)
        {
            _allomorphs.Remove(id);
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
        public ICollection<WordSynthesis> MorphAndLookupWord(string word)
        {
            WordAnalysisTrace trace;
            return MorphAndLookupWord(word, out trace);
        }

        public ICollection<WordSynthesis> MorphAndLookupWord(string word, out WordAnalysisTrace trace)
        {
            return MorphAndLookupToken(word, null, null, out trace);
        }

        /// <summary>
        /// Morphs the list of specified words.
        /// </summary>
        /// <param name="wordList">The word list.</param>
        /// <returns>All valid word synthesis records for each word.</returns>
        public IList<ICollection<WordSynthesis>> MorphAndLookupWordList(IList<string> wordList)
        {
            IList<WordAnalysisTrace> traces;
            return MorphAndLookupWordList(wordList, out traces);
        }

        public IList<ICollection<WordSynthesis>> MorphAndLookupWordList(IList<string> wordList,
            out IList<WordAnalysisTrace> traces)
        {
            var results = new List<ICollection<WordSynthesis>>();
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
        ICollection<WordSynthesis> MorphAndLookupToken(string word, string prev, string next, out WordAnalysisTrace trace)
        {
            // convert the word to its phonetic shape
            Shape input = SurfaceStratum.CharacterDefinitionTable.ToShape(word);
            // if word contains invalid segments, the char def table will return null
			if (input == null)
			{
				var me = new MorphException(MorphErrorCode.InvalidShape,
					string.Format(HCStrings.kstidInvalidWord, word, SurfaceStratum.CharacterDefinitionTable.ID));
				me.Data["shape"] = word;
				me.Data["charDefTable"] = SurfaceStratum.CharacterDefinitionTable.ID;
				throw me;
			}

            // create the root of the trace tree
            trace = new WordAnalysisTrace(word, input);

            var candidates = new HashSet<WordSynthesis>();
            var inAnalysis = new HashSet<WordAnalysis>();
            var outAnalysis = new HashSet<WordAnalysis>();
            inAnalysis.Add(new WordAnalysis(input, SurfaceStratum, trace));

            // Unapply rules
            for (int i = _orderedStrata.Count - 1; i >= 0; i--)
            {
                outAnalysis.Clear();
                foreach (WordAnalysis wa in inAnalysis)
                {
                    if (_traceStrataAnalysis)
                    {
                        // create the stratum analysis input trace record
                        var stratumTrace = new StratumAnalysisTrace(_orderedStrata[i], true, wa.Clone());
                        wa.CurrentTrace.AddChild(stratumTrace);
                    }
                    foreach (WordAnalysis outWa in _orderedStrata[i].Unapply(wa, candidates))
                    {
                        // promote each analysis to the next stratum
                        if (i != 0)
                            outWa.Stratum = _orderedStrata[i - 1];

                        if (_traceStrataAnalysis)
                            // create the stratum analysis output trace record for the output word synthesis
                            outWa.CurrentTrace.AddChild(new StratumAnalysisTrace(_orderedStrata[i], false, outWa.Clone()));

                        outAnalysis.Add(outWa);
                    }
                }

                inAnalysis.Clear();
                inAnalysis.UnionWith(outAnalysis);
            }

			var allValidSyntheses = new HashSet<WordSynthesis>();
            // Apply rules for each candidate entry
            foreach (WordSynthesis candidate in candidates)
            {
                var inSynthesis = new HashSet<WordSynthesis>();
                var outSynthesis = new HashSet<WordSynthesis>();
                for (int i = 0; i < _orderedStrata.Count; i++)
                {
                    // start applying at the stratum that this lex entry belongs to
                    if (_orderedStrata[i] == candidate.Root.Stratum)
                        inSynthesis.Add(candidate);

                    outSynthesis.Clear();
                    foreach (WordSynthesis cur in inSynthesis)
                    {
                        if (_traceStrataSynthesis)
                        {
                            // create the stratum synthesis input trace record
                            var stratumTrace = new StratumSynthesisTrace(_orderedStrata[i], true, cur.Clone());
                            cur.CurrentTrace.AddChild(stratumTrace);
                        }
                        foreach (WordSynthesis outWs in _orderedStrata[i].Apply(cur))
                        {
                            // promote the word synthesis to the next stratum
                            if (i != _orderedStrata.Count - 1)
                                outWs.Stratum = _orderedStrata[i + 1];

                            if (_traceStrataSynthesis)
                                // create the stratum synthesis output trace record for the output analysis
                                outWs.CurrentTrace.AddChild(new StratumSynthesisTrace(_orderedStrata[i], false, outWs.Clone()));

                            outSynthesis.Add(outWs);
                        }
                    }

                    inSynthesis.Clear();
                    inSynthesis.UnionWith(outSynthesis);
                }

				foreach (WordSynthesis ws in outSynthesis)
				{
					if (ws.IsValid)
						allValidSyntheses.Add(ws);
				}
            }

			var results = new HashSet<WordSynthesis>();
			// sort the resulting syntheses according to the order of precedence of each allomorph in
			// their respective morphemes
			var sortedSyntheses = new List<WordSynthesis>(allValidSyntheses);
			sortedSyntheses.Sort();

			WordSynthesis prevValidSynthesis = null;
			foreach (WordSynthesis cur in sortedSyntheses)
			{
				// enforce the disjunctive property of allomorphs by ensuring that this word synthesis
				// has the highest order of precedence for its allomorphs, also check that the phonetic
				// shape matches the original input word
				if ((prevValidSynthesis == null || !cur.Morphs.SameMorphemes(prevValidSynthesis.Morphs))
					&& SurfaceStratum.CharacterDefinitionTable.IsMatch(word, cur.Shape))
				{
					if (_traceSuccess)
						// create the report a success output trace record for the output analysis
						cur.CurrentTrace.AddChild(new ReportSuccessTrace(cur));
					// do not add to the result if it has the same root, shape, and morphemes as another result
					if (!results.Any(ws => cur.Duplicates(ws)))
						results.Add(cur);
				}
				prevValidSynthesis = cur;
			}
            return results;
        }
    }
}
