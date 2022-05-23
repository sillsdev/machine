using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    public enum MorphologicalRuleOrder
    {
        Linear,
        Unordered
    }

    /// <summary>
    /// This class encapsulates the character definition table, rules, and lexicon for
    /// a particular stratum.
    /// </summary>
    public class Stratum : HCRuleBase
    {
        private readonly CharacterDefinitionTable _charDefTable;

        private readonly ObservableCollection<IMorphologicalRule> _mrules;
        private readonly List<IPhonologicalRule> _prules;

        private readonly ObservableCollection<AffixTemplate> _templates;

        private readonly ObservableCollection<LexEntry> _entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="Stratum"/> class.
        /// </summary>
        /// <param name="charDefTable"></param>
        public Stratum(CharacterDefinitionTable charDefTable)
        {
            Depth = -1;
            _charDefTable = charDefTable;

            _mrules = new ObservableCollection<IMorphologicalRule>();
            _mrules.CollectionChanged += MorphologicalRulesChanged;
            _prules = new List<IPhonologicalRule>();

            _templates = new ObservableCollection<AffixTemplate>();
            _templates.CollectionChanged += TemplatesChanged;

            _entries = new ObservableCollection<LexEntry>();
            _entries.CollectionChanged += EntriesChanged;
        }

        private void MorphologicalRulesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (IMorphologicalRule mr in e.OldItems)
                    mr.Stratum = null;
            }
            if (e.NewItems != null)
            {
                foreach (IMorphologicalRule mr in e.NewItems)
                    mr.Stratum = this;
            }
        }

        private void TemplatesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (AffixTemplate template in e.OldItems)
                    template.Stratum = null;
            }
            if (e.NewItems != null)
            {
                foreach (AffixTemplate template in e.NewItems)
                    template.Stratum = this;
            }
        }

        private void EntriesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (LexEntry entry in e.OldItems)
                    entry.Stratum = null;
            }
            if (e.NewItems != null)
            {
                foreach (LexEntry entry in e.NewItems)
                    entry.Stratum = this;
            }
        }

        /// <summary>
        /// Gets the character definition table.
        /// </summary>
        public CharacterDefinitionTable CharacterDefinitionTable
        {
            get { return _charDefTable; }
        }

        public IList<IMorphologicalRule> MorphologicalRules
        {
            get { return _mrules; }
        }

        public IList<IPhonologicalRule> PhonologicalRules
        {
            get { return _prules; }
        }

        public ICollection<AffixTemplate> AffixTemplates
        {
            get { return _templates; }
        }

        public ICollection<LexEntry> Entries
        {
            get { return _entries; }
        }

        public int Depth { get; internal set; }

        /// <summary>
        /// Gets or sets the morphological rule order.
        /// </summary>
        /// <value>The morphological rule order.</value>
        public MorphologicalRuleOrder MorphologicalRuleOrder { get; set; }

        public override IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher)
        {
            return new AnalysisStratumRule(morpher, this);
        }

        public override IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher)
        {
            return new SynthesisStratumRule(morpher, this);
        }
    }
}
