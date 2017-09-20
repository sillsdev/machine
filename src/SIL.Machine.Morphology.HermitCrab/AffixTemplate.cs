using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
	/// <summary>
	/// This class represents an affix template. It is normally used to model inflectional
	/// affixation.
	/// </summary>
	public class AffixTemplate : HCRuleBase
	{
		private Stratum _stratum;
		private readonly ObservableCollection<AffixTemplateSlot> _slots;

		/// <summary>
		/// Initializes a new instance of the <see cref="AffixTemplate"/> class.
		/// </summary>
		public AffixTemplate()
		{
			_slots = new ObservableCollection<AffixTemplateSlot>();
			_slots.CollectionChanged += SlotsChanged;
			RequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
			IsFinal = true;
		}

		private void SlotsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (AffixTemplateSlot slot in e.OldItems)
				{
					foreach (MorphemicMorphologicalRule rule in slot.Rules)
					{
						rule.Stratum = null;
						rule.IsTemplateRule = false;
					}
				}
			}
			if (e.NewItems != null)
			{
				foreach (AffixTemplateSlot slot in e.NewItems)
				{
					foreach (MorphemicMorphologicalRule rule in slot.Rules)
					{
						rule.Stratum = Stratum;
						rule.IsTemplateRule = true;
					}
				}
			}
		}

		public FeatureStruct RequiredSyntacticFeatureStruct { get; set; }

		public bool IsFinal { get; set; }

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
					foreach (MorphemicMorphologicalRule rule in slot.Rules)
						rule.Stratum = value;
				}
			}
		}

		public override IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher)
		{
			return new AnalysisAffixTemplateRule(morpher, this);
		}

		public override IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher)
		{
			return new SynthesisAffixTemplateRule(morpher, this);
		}
	}
}