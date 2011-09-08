using System;
using System.Collections.Generic;
using SIL.APRE;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class encapsulates the character definition table, rules, and lexicon for
    /// a particular stratum.
    /// </summary>
    public class Stratum : IDBearerBase
    {
        /// <summary>
        /// This enumeration represents the rule ordering for phonological rules.
        /// </summary>
        public enum PRuleOrder { Linear, Simultaneous };

        /// <summary>
        /// This enumeration represents the rule ordering for morphological rules.
        /// </summary>
        public enum MRuleOrder { Linear, Unordered };

        /// <summary>
        /// The surface stratum ID
        /// </summary>
        public const string SurfaceStratumID = "surface";

        private CharacterDefinitionTable _charDefTable;
        private bool _isCyclic;
        private PRuleOrder _pruleOrder = PRuleOrder.Linear;
        private MRuleOrder _mruleOrder = MRuleOrder.Linear;

    	private readonly SegmentDefinitionTrie<LexEntry.RootAllomorph> _entryTrie;
        private readonly List<MorphologicalRule> _mrules;
        private readonly List<PhonologicalRule> _prules;
        private readonly IDBearerSet<AffixTemplate> _templates;

        /// <summary>
        /// Initializes a new instance of the <see cref="Stratum"/> class.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <param name="desc">The description.</param>
        public Stratum(string id, string desc)
            : base(id, desc)
        {
            _mrules = new List<MorphologicalRule>();
            _prules = new List<PhonologicalRule>();
            _templates = new IDBearerSet<AffixTemplate>();
            _entryTrie = new SegmentDefinitionTrie<LexEntry.RootAllomorph>(Direction.LeftToRight);
        }

        /// <summary>
        /// Gets or sets the character definition table.
        /// </summary>
        /// <value>The character definition table.</value>
        public CharacterDefinitionTable CharacterDefinitionTable
        {
            get
            {
                return _charDefTable;
            }

            set
            {
                _charDefTable = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is cyclic.
        /// </summary>
        /// <value><c>true</c> if this instance is cyclic; otherwise, <c>false</c>.</value>
        public bool IsCyclic
        {
            get
            {
                return _isCyclic;
            }

            set
            {
                _isCyclic = value;
            }
        }

        /// <summary>
        /// Gets or sets the phonological rule order.
        /// </summary>
        /// <value>The phonological rule order.</value>
        public PRuleOrder PhonologicalRuleOrder
        {
            get
            {
                return _pruleOrder;
            }

            set
            {
                _pruleOrder = value;
            }
        }

        /// <summary>
        /// Gets or sets the morphological rule order.
        /// </summary>
        /// <value>The morphological rule order.</value>
        public MRuleOrder MorphologicalRuleOrder
        {
            get
            {
                return _mruleOrder;
            }

            set
            {
                _mruleOrder = value;
            }
        }

        /// <summary>
        /// Gets the affix templates.
        /// </summary>
        /// <value>The affix templates.</value>
        public IEnumerable<AffixTemplate> AffixTemplates
        {
            get
            {
                return _templates;
            }
        }

        /// <summary>
        /// Adds the phonological rule.
        /// </summary>
        /// <param name="prule">The phonological rule.</param>
        public void AddPhonologicalRule(PhonologicalRule prule)
        {
            _prules.Add(prule);
        }

        /// <summary>
        /// Removes the phonological rule associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemovePhonologicalRule(string id)
        {
        	_prules.RemoveAll(rule => rule.ID == id);
        }

        /// <summary>
        /// Adds the morphological rule.
        /// </summary>
        /// <param name="mrule">The morphological rule.</param>
        public void AddMorphologicalRule(MorphologicalRule mrule)
        {
            mrule.Stratum = this;
            _mrules.Add(mrule);
        }

        /// <summary>
        /// Removes the morphological rule associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        public void RemoveMorphologicalRule(string id)
        {
        	_mrules.RemoveAll(rule => rule.ID == id);
        }

        /// <summary>
        /// Adds the lexical entry.
        /// </summary>
        /// <param name="entry">The lexical entry.</param>
        public void AddEntry(LexEntry entry)
        {
            entry.Stratum = this;
            foreach (LexEntry.RootAllomorph allomorph in entry.Allomorphs)
                _entryTrie.Add(allomorph.Shape, allomorph);
        }

		/// <summary>
		/// Searches for the lexical entry that matches the specified shape.
		/// </summary>
		/// <param name="shape">The shape.</param>
		/// <returns>The matching lexical entries.</returns>
		public IEnumerable<LexEntry.RootAllomorph> SearchEntries(PhoneticShape shape)
		{
			foreach (SegmentDefinitionTrie<LexEntry.RootAllomorph>.Match match in _entryTrie.Search(shape))
				yield return match.Value;
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
        /// Clears the affix templates.
        /// </summary>
        public void ClearAffixTemplates()
        {
            _templates.Clear();
        }

        /// <summary>
        /// Unapplies all of the rules to the specified input word analysis. All matching lexical
        /// entries are added to the <c>candidates</c> parameter.
        /// </summary>
        /// <param name="input">The input word analysis.</param>
        /// <param name="candidates">The set of candidate word synthesis records.</param>
        /// <returns>All word analyses that result from the unapplication of rules.</returns>
        public IEnumerable<WordAnalysis> Unapply(WordAnalysis input, ICollection<WordSynthesis> candidates)
        {
            if (_isCyclic)
                throw new NotImplementedException(HCStrings.kstidCyclicStratumNotSupported);

            if (_pruleOrder == PRuleOrder.Simultaneous)
                throw new NotImplementedException(HCStrings.kstidSimultOrderNotSupported);

            WordAnalysis wa = input.Clone();

            UnapplyPhonologicalRules(wa);

            LexicalLookup(wa, candidates);

            var tempOutput = new HashSet<WordAnalysis>();
            tempOutput.Add(wa);
            UnapplyTemplates(wa, tempOutput, candidates);

            var output = new HashSet<WordAnalysis>();
            // TODO: handle cyclicity
            foreach (WordAnalysis analysis in tempOutput)
                UnapplyMorphologicalRules(analysis, _mrules.Count - 1, 0, candidates, output);

            return output;
        }

        void UnapplyMorphologicalRules(WordAnalysis input, int rIndex, int srIndex, ICollection<WordSynthesis> candidates,
            HashSet<WordAnalysis> output)
        {
            // first try to unapply the specified subrule
            bool unapplied = false;
            if (rIndex >= 0)
            {
                if (_mrules[rIndex].BeginUnapplication(input))
                {
                    ICollection<WordAnalysis> analyses;
                    if (_mrules[rIndex].Unapply(input, srIndex, out analyses))
                    {
                        foreach (WordAnalysis wa in analyses)
                            MorphologicalRuleUnapplied(wa, rIndex, srIndex, candidates, output);

                        unapplied = true;
                    }
                }
                else
                {
                    // move to next rule
                    rIndex--;
                    srIndex = -1;
                }
            }

            // iterate thru all subrules that occur after the specified rule in analysis order
            for (int i = rIndex; i >= 0; i--)
            {
                if (srIndex != -1 || _mrules[i].BeginUnapplication(input))
                {
                    for (int j = 0; j < _mrules[i].SubruleCount; j++)
                    {
                        // skip the specified subrule, since we already tried to unapply it
                        if (j != srIndex)
                        {
                            ICollection<WordAnalysis> analyses;
                            if (_mrules[i].Unapply(input, j, out analyses))
                            {
                                foreach (WordAnalysis wa in analyses)
                                    MorphologicalRuleUnapplied(wa, i, j, candidates, output);

                                unapplied = true;
                            }
                        }
                    }

					_mrules[i].EndUnapplication(input, unapplied);
                }

                unapplied = false;
                srIndex = -1;
            }

            output.Add(input);
        }

        void MorphologicalRuleUnapplied(WordAnalysis ruleOutput, int rIndex, int srIndex, ICollection<WordSynthesis> candidates,
			HashSet<WordAnalysis> output)
        {
            if (ruleOutput.Shape.Count > 2)
            {
                // lookup resulting phonetic shape in lexicon
                LexicalLookup(ruleOutput, candidates);

                // recursive call so that we can cover every permutation of rule unapplication
                switch (_mruleOrder)
                {
                    case MRuleOrder.Linear:
                        UnapplyMorphologicalRules(ruleOutput, rIndex, srIndex, candidates, output);
                        break;

                    case MRuleOrder.Unordered:
                        // start over from the very beginning
                        UnapplyMorphologicalRules(ruleOutput, _mrules.Count - 1, 0, candidates, output);
                        break;
                }
            }
        }

        void UnapplyTemplates(WordAnalysis input, HashSet<WordAnalysis> output, ICollection<WordSynthesis> candidates)
        {
            foreach (AffixTemplate template in _templates)
            {
                if (template.IsUnapplicable(input))
                {
                    IEnumerable<WordAnalysis> tempOutput;
                    if (template.Unapply(input, out tempOutput))
                    {
                        foreach (WordAnalysis tempAnalysis in tempOutput)
                        {
                            // don't bother to do a lookup if this analysis already exists
                            if (!output.Contains(tempAnalysis))
                            {
                                output.Add(tempAnalysis);
                                // lookup resulting phonetic shape in lexicon
                                LexicalLookup(tempAnalysis, candidates);
                            }
                        }
                    }
                }
            }
        }

        void LexicalLookup(WordAnalysis input, ICollection<WordSynthesis> candidates)
        {
            LexLookupTrace lookupTrace = null;
#if WANTPORT
            if (Morpher.TraceLexLookup)
            {
                // create lexical lookup trace record
                lookupTrace = new LexLookupTrace(this, input.Shape.Clone());
                input.CurrentTrace.AddChild(lookupTrace);
            }
#endif

            foreach (SegmentDefinitionTrie<LexEntry.RootAllomorph>.Match match in _entryTrie.Search(input.Shape))
            {
                // don't allow a compound where both roots are the same
                if (input.NonHead == null || input.NonHead.RootAllomorph.Morpheme != match.Value.Morpheme)
                {
                	LexEntry entry = (LexEntry) match.Value.Morpheme;
					foreach (LexEntry.RootAllomorph allomorph in entry.Allomorphs)
					{
						WordAnalysis wa = input.Clone();

						wa.RootAllomorph = allomorph;

#if WANTPORT
						if (Morpher.TraceLexLookup)
						{
							// successful lookup, so create word synthesis trace record
							var wsTrace = new WordSynthesisTrace(wa.RootAllomorph, wa.UnappliedMorphologicalRules,
								(FeatureStructure) wa.RealizationalFeatures.Clone());
							lookupTrace.AddChild(wsTrace);
							wa.CurrentTrace = wsTrace;
						}
#endif

						candidates.Add(new WordSynthesis(wa));
					}
                }
            }
        }

        void UnapplyPhonologicalRules(WordAnalysis input)
        {
            // TODO: handle ordering properly
            //for (int i = _prules.Count - 1; i >= 0; i--)
            //    _prules[i].Unapply(input);
        }

        /// <summary>
        /// Applies all of the rules to the specified word synthesis.
        /// </summary>
        /// <param name="input">The input word synthesis.</param>
        /// <returns>All word synthesis records that result from the application of rules.</returns>
        public IEnumerable<WordSynthesis> Apply(WordSynthesis input)
        {
            if (_isCyclic)
                throw new NotImplementedException(HCStrings.kstidCyclicStratumNotSupported);

            if (_pruleOrder == PRuleOrder.Simultaneous)
                throw new NotImplementedException(HCStrings.kstidSimultOrderNotSupported);

            // TODO: handle cyclicity
            var output = new HashSet<WordSynthesis>();
            ApplyMorphologicalRules(input.Clone(), 0, output);

            foreach (WordSynthesis cur in output)
                ApplyPhonologicalRules(cur);

            return output;
        }

        void ApplyMorphologicalRules(WordSynthesis input, int rIndex, HashSet<WordSynthesis> output)
        {
            // iterate thru all rules starting from the specified rule in synthesis order
            for (int i = rIndex; i < _mrules.Count; i++)
            {
				if (_mrules[i].IsApplicable(input))
				{
					ICollection<WordSynthesis> syntheses;
					if (_mrules[i].Apply(input, out syntheses))
					{
						foreach (WordSynthesis ws in syntheses)
						{
							// recursive call so that we can cover every permutation of rule application
							switch (_mruleOrder)
							{
								case MRuleOrder.Linear:
									ApplyMorphologicalRules(ws, i, output);
									break;

								case MRuleOrder.Unordered:
									ApplyMorphologicalRules(ws, 0, output);
									break;
							}
						}
					}
				}
            }

            ApplyTemplates(input, output);
        }

        void ApplyTemplates(WordSynthesis input, HashSet<WordSynthesis> output)
        {
            // if this word synthesis is not compatible with the realizational features,
            // then skip it
#if WANTPORT
            if (!input.RealizationalFeatures.IsCompatible(input.HeadFeatures))
                return;
#endif

            WordSynthesis ws = ChooseInflStem(input);
            bool applicableTemplate = false;
            foreach (AffixTemplate template in _templates)
            {
                // HC.NET does not treat templates as applying disjunctively, as opposed to legacy HC,
                // which does
                if (template.IsApplicable(ws))
                {
                    applicableTemplate = true;
                    IEnumerable<WordSynthesis> tempOutput;
                    if (template.Apply(ws, out tempOutput))
                        output.UnionWith(tempOutput);
                }
            }

            if (!applicableTemplate)
                output.Add(ws);
        }

        /// <summary>
        /// If the list of Realizational Features is non-empty, choose from either the input stem or its relatives
        /// of this stratum that stem which incorporates the most realizational features (without being incompatible
        /// with any realizational feature or with the head and foot features of the input stem).
        /// </summary>
        /// <param name="ws">The input word synthesis.</param>
        /// <returns>The resulting word synthesis.</returns>
        WordSynthesis ChooseInflStem(WordSynthesis ws)
        {
            if (ws.RealizationalFeatures.NumValues == 0 || ws.Root.Family == null)
                return ws;

            WordSynthesis best = ws;
#if WANTPORT
            // iterate thru all relatives
            foreach (LexEntry relative in ws.Root.Family.Entries)
            {
                if (relative != ws.Root && relative.Stratum == ws.Stratum
                    && ws.RealizationalFeatures.IsCompatible(relative.HeadFeatures)
                    && ws.PartOfSpeech == relative.PartOfSpeech && relative.FootFeatures.Equals(ws.FootFeatures))
                {
                    FeatureValues remainder;
                    if (best.HeadFeatures.GetSupersetRemainder(relative.HeadFeatures, out remainder) && remainder.NumFeatures > 0
                        && ws.RealizationalFeatures.IsCompatible(remainder))
                    {
                        if (Morpher.TraceBlocking)
                            // create blocking trace record, should this become the current trace?
                            ws.CurrentTrace.AddChild(new BlockingTrace(BlockingTrace.BlockType.Template, relative));
                        best = new WordSynthesis(relative.PrimaryAllomorph, ws.RealizationalFeatures, ws.CurrentTrace);
                    }
                }
            }
#endif
            return best;
        }

        void ApplyPhonologicalRules(WordSynthesis input)
        {
            //for (int i = 0; i < _prules.Count; i++)
            //    _prules[i].Apply(input);
        }
    }
}
