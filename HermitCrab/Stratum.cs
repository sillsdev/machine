using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class encapsulates the character definition table, rules, and lexicon for
    /// a particular stratum.
    /// </summary>
    public class Stratum : IDBearerBase, IHCRule
    {
        private readonly SymbolTable _symDefTable;

    	private readonly ObservableCollection<IMorphologicalRule> _mrules;
        private readonly List<IPhonologicalRule> _prules;

        private readonly ObservableCollection<AffixTemplate> _templates;

    	private readonly ObservableCollection<LexEntry> _entries;

    	/// <summary>
    	/// Initializes a new instance of the <see cref="Stratum"/> class.
    	/// </summary>
    	/// <param name="id">The ID.</param>
    	/// <param name="symDefTable"></param>
    	public Stratum(string id, SymbolTable symDefTable)
            : base(id)
    	{
    		_symDefTable = symDefTable;

			_mrules = new ObservableCollection<IMorphologicalRule>();
			_mrules.CollectionChanged += MorphologicalRulesChanged;
            _prules = new List<IPhonologicalRule>();

			MorphologicalRuleOrder = RuleCascadeOrder.Permutation;

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
        /// Gets the symbol definition table.
        /// </summary>
        /// <value>The symbol definition table.</value>
        public SymbolTable SymbolTable
        {
            get
            {
                return _symDefTable;
            }
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

    	/// <summary>
    	/// Gets or sets the phonological rule order.
    	/// </summary>
    	/// <value>The phonological rule order.</value>
		public RuleCascadeOrder PhonologicalRuleOrder { get; set; }

    	/// <summary>
    	/// Gets or sets the morphological rule order.
    	/// </summary>
    	/// <value>The morphological rule order.</value>
		public RuleCascadeOrder MorphologicalRuleOrder { get; set; }

    	public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
    	{
			// TODO: support derivation outside of inflection
			var pruleAnalysisRule = new RuleCascade<Word, ShapeNode>(_prules.Select(prule => prule.CompileAnalysisRule(spanFactory, morpher)).Reverse(), PhonologicalRuleOrder);
			var templateAnalysisRule = new RuleCascade<Word, ShapeNode>(_templates.Select(template => template.CompileAnalysisRule(spanFactory, morpher)));
			var mruleAnalysisRule = new RuleCascade<Word, ShapeNode>(_mrules.Select(mrule => mrule.CompileAnalysisRule(spanFactory, morpher)).Reverse(), MorphologicalRuleOrder, true);
			return new RuleCascade<Word, ShapeNode>(new IRule<Word, ShapeNode>[] { pruleAnalysisRule, templateAnalysisRule, mruleAnalysisRule });
    	}

    	public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
    	{
			// TODO: support derivation outside of inflection
			var mruleSynthesisRule = new RuleCascade<Word, ShapeNode>(_mrules.Select(mrule => mrule.CompileSynthesisRule(spanFactory, morpher)), MorphologicalRuleOrder, true);
			var templateSynthesisRule = new SynthesisAffixTemplatesRule(spanFactory, morpher, this);
			var pruleSynthesisRule = new RuleCascade<Word, ShapeNode>(_prules.Select(prule => prule.CompileSynthesisRule(spanFactory, morpher)), PhonologicalRuleOrder);
			return new RuleCascade<Word, ShapeNode>(new IRule<Word, ShapeNode>[] { mruleSynthesisRule, templateSynthesisRule, pruleSynthesisRule });
    	}
    }
}
