using System.Collections.Generic;
using SIL.APRE;
using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents an affix template. It is normally used to model inflectional
    /// affixation.
    /// </summary>
    public class AffixTemplate : IDBearerBase
    {
        private readonly List<Slot> _slots;
        private IDBearerSet<PartOfSpeech> _requiredPartsOfSpeech;

        /// <summary>
        /// Initializes a new instance of the <see cref="AffixTemplate"/> class.
        /// </summary>
        /// <param name="id">The ID.</param>
        public AffixTemplate(string id)
            : base(id)
        {
            _slots = new List<Slot>();
        }

        /// <summary>
        /// Gets or sets the required parts of speech.
        /// </summary>
        /// <value>The required parts of speech.</value>
        public IEnumerable<PartOfSpeech> RequiredPartsOfSpeech
        {
            get
            {
                return _requiredPartsOfSpeech;
            }

            set
            {
                _requiredPartsOfSpeech = new IDBearerSet<PartOfSpeech>(value);
            }
        }

        /// <summary>
        /// Adds the slot.
        /// </summary>
        /// <param name="slot">The slot.</param>
        public void AddSlot(Slot slot)
        {
            _slots.Add(slot);
        }

        public bool IsUnapplicable(WordAnalysis input)
        {
            foreach (PartOfSpeech pos in _requiredPartsOfSpeech)
            {
                if (input.MatchPartOfSpeech(pos))
                    return true;
            }
            return false;
        }

		/// <summary>
		/// Unapplies this affix template to specified input word analysis.
		/// </summary>
		/// <param name="input">The input word analysis.</param>
		/// <param name="output">The output word analyses.</param>
		/// <returns>The resulting word analyses.</returns>
        public bool Unapply(WordAnalysis input, out IEnumerable<WordAnalysis> output)
        {
            var results = new HashSet<WordAnalysis>();
#if WANTPORT
            if (Morpher.TraceTemplatesAnalysis)
            {
                // create the template analysis trace input record
                var tempTrace = new TemplateAnalysisTrace(this, true, input.Clone());
                input.CurrentTrace.AddChild(tempTrace);
            }
#endif
            UnapplySlots(input.Clone(), _slots.Count - 1, results);
            foreach (WordAnalysis wa in results)
            {
                foreach (PartOfSpeech pos in _requiredPartsOfSpeech)
                    wa.AddPartOfSpeech(pos);
            }

            if (results.Count > 0)
            {
                output = results;
                return true;
            }
            else
            {
                output = null;
                return false;
            }
        }

        void UnapplySlots(WordAnalysis input, int sIndex, HashSet<WordAnalysis> output)
        {
            for (int i = sIndex; i >= 0; i--)
            {
                foreach (MorphologicalRule rule in _slots[i].MorphologicalRules)
                {
#if WANTPORT
                    if (rule.BeginUnapplication(input))
                    {
                        bool ruleUnapplied = false;
                        for (int j = 0; j < rule.SubruleCount; j++)
                        {
                            ICollection<WordAnalysis> analyses;
                            if (rule.Unapply(input, j, out analyses))
                            {
                                ruleUnapplied = true;
                                foreach (WordAnalysis wa in analyses)
                                {
                                    if (wa.Shape.Count > 2)
                                        UnapplySlots(wa, i - 1, output);
                                }
                            }
                        }
						rule.EndUnapplication(input, ruleUnapplied);
                    }
#endif
                }
                // we can skip this slot if it is optional
                if (!_slots[i].IsOptional)
                {
#if WANTPORT
                    if (Morpher.TraceTemplatesAnalysis)
                        input.CurrentTrace.AddChild(new TemplateAnalysisTrace(this, false, null));
#endif
                    return;
                }
            }

#if WANTPORT
            if (Morpher.TraceTemplatesAnalysis)
                input.CurrentTrace.AddChild(new TemplateAnalysisTrace(this, false, input.Clone()));
#endif
            output.Add(input);
        }

        public bool IsApplicable(WordSynthesis input)
        {
            return _requiredPartsOfSpeech.Contains(input.PartOfSpeech);
        }

        /// <summary>
        /// Applies this affix template to the specified input word synthesis.
        /// </summary>
        /// <param name="input">The input word synthesis.</param>
        /// <param name="output">The output word synthesis.</param>
        /// <returns><c>true</c> if the affix template applied, otherwise <c>false</c>.</returns>
        public bool Apply(WordSynthesis input, out IEnumerable<WordSynthesis> output)
        {
            var headFeatures = (FeatureStruct) input.HeadFeatures.Clone();
            var results = new HashSet<WordSynthesis>();
#if WANTPORT
            if (Morpher.TraceTemplatesSynthesis)
            {
                // create the template synthesis input trace record
                var tempTrace = new TemplateSynthesisTrace(this, true, input.Clone());
                input.CurrentTrace.AddChild(tempTrace);
            }
#endif
            ApplySlots(input.Clone(), 0, headFeatures, results);

            if (results.Count > 0)
            {
                output = results;
                return true;
            }
            else
            {
                output = null;
                return false;
            }
        }

        void ApplySlots(WordSynthesis input, int sIndex, FeatureStruct origHeadFeatures, HashSet<WordSynthesis> output)
        {
            for (int i = sIndex; i < _slots.Count; i++)
            {
                foreach (MorphologicalRule rule in _slots[i].MorphologicalRules)
                {
#if WANTPORT
                    if (rule.IsApplicable(input))
                    {
                        // this is the slot affix that realizes the features
                        ICollection<WordSynthesis> syntheses;
						if (rule.ApplySlotAffix(input, origHeadFeatures, out syntheses))
						{
							foreach (WordSynthesis ws in syntheses)
								ApplySlots(ws, i + 1, origHeadFeatures, output);
						}
                    }
#endif
                }

                if (!_slots[i].IsOptional)
                {
#if WANTPORT
                    if (Morpher.TraceTemplatesSynthesis)
                        input.CurrentTrace.AddChild(new TemplateSynthesisTrace(this, false, null));
#endif
                    return;
                }
            }

#if WANTPORT
            if (Morpher.TraceTemplatesSynthesis)
                input.CurrentTrace.AddChild(new TemplateSynthesisTrace(this, false, input.Clone()));
#endif
            output.Add(input);
        }
    }
}