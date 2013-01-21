using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents an affix template. It is normally used to model inflectional
    /// affixation.
    /// </summary>
    public class AffixTemplate : IDBearerBase, IHCRule
    {
    	private Stratum _stratum;
        private readonly ObservableCollection<AffixTemplateSlot> _slots;

        /// <summary>
        /// Initializes a new instance of the <see cref="AffixTemplate"/> class.
        /// </summary>
        /// <param name="id">The ID.</param>
        public AffixTemplate(string id)
            : base(id)
        {
            _slots = new ObservableCollection<AffixTemplateSlot>();
			_slots.CollectionChanged += SlotsChanged;
			RequiredSyntacticFeatureStruct = new FeatureStruct();
        }

		private void SlotsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (AffixTemplateSlot slot in e.OldItems)
				{
					foreach (IMorphologicalRule rule in slot.Rules)
						rule.Stratum = null;
				}
			}
			if (e.NewItems != null)
			{
				foreach (AffixTemplateSlot slot in e.NewItems)
				{
					foreach (IMorphologicalRule rule in slot.Rules)
						rule.Stratum = Stratum;
				}
			}
		}

		public FeatureStruct RequiredSyntacticFeatureStruct { get; set; }

    	public IList<AffixTemplateSlot> Slots
    	{
    		get { return _slots; }
    	}

    	public Stratum Stratum
    	{
    		get { return _stratum; }
			set
			{
				_stratum = value;
				foreach (AffixTemplateSlot slot in _slots)
				{
					foreach (IMorphologicalRule rule in slot.Rules)
						rule.Stratum = value;
				}
			}
    	}

    	public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
    	{
    		return new AnalysisAffixTemplateRule(spanFactory, morpher, this);
    	}

    	public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
    	{
    		return new SynthesisAffixTemplateRule(spanFactory, morpher, this);
    	}

    	public void Traverse(Action<IHCRule> action)
    	{
    		action(this);
			foreach (AffixTemplateSlot slot in _slots)
			{
				foreach (IMorphologicalRule rule in slot.Rules)
					rule.Traverse(action);
			}
    	}
    }
}