using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
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
    		Depth = -1;
    		_symDefTable = symDefTable;

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

    	public int Depth { get; internal set; }

    	/// <summary>
    	/// Gets or sets the morphological rule order.
    	/// </summary>
    	/// <value>The morphological rule order.</value>
		public MorphologicalRuleOrder MorphologicalRuleOrder { get; set; }

    	public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
    	{
			return new AnalysisStratumRule(spanFactory, morpher, this);
    	}

    	public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
    	{
			return new SynthesisStratumRule(spanFactory, morpher, this);
    	}

    	public void Traverse(Action<IHCRule> action)
    	{
    		action(this);
			foreach (IHCRule rule in _mrules.Cast<IHCRule>().Concat(_prules).Concat(_templates))
				rule.Traverse(action);
    	}
    }
}
