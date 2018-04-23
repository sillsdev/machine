using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SIL.Machine.Morphology.HermitCrab
{
	/// <summary>
	/// This class represents a morpheme. All morpheme objects should extend this class.
	/// </summary>
	public abstract class Morpheme : IMorpheme
	{
		private readonly ObservableCollection<MorphemeCoOccurrenceRule> _morphemeCoOccurrenceRules;
		private readonly Properties _properties;

		/// <summary>
		/// Initializes a new instance of the <see cref="Morpheme"/> class.
		/// </summary>
		protected Morpheme()
		{
			_morphemeCoOccurrenceRules = new ObservableCollection<MorphemeCoOccurrenceRule>();
			_morphemeCoOccurrenceRules.CollectionChanged += MorphemeCoOccurrenceRuleRulesChanged;
			_properties = new Properties();
		}

		private void MorphemeCoOccurrenceRuleRulesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (MorphemeCoOccurrenceRule cooccur in e.OldItems)
					cooccur.Key = null;
			}
			if (e.NewItems != null)
			{
				foreach (MorphemeCoOccurrenceRule cooccur in e.NewItems)
					cooccur.Key = this;
			}
		}

		/// <summary>
		/// Gets or sets the stratum.
		/// </summary>
		/// <value>The stratum.</value>
		public Stratum Stratum { get; set; }

		public string Id { get; set; }

		public abstract string Category { get; }

		/// <summary>
		/// Gets or sets the morpheme's gloss.
		/// </summary>
		/// <value>The gloss.</value>
		public string Gloss { get; set; }

		public abstract MorphemeType MorphemeType { get; }

		/// <summary>
		/// Gets or sets a value indicating whether this morpheme is partially analyzed.
		/// </summary>
		public bool IsPartial { get; set; }

		public abstract Allomorph GetAllomorph(int index);

		/// <summary>
		/// Gets the morpheme co-occurrence rules.
		/// </summary>
		/// <value>The morpheme co-occurrence rules.</value>
		public ICollection<MorphemeCoOccurrenceRule> MorphemeCoOccurrenceRules
		{
			get { return _morphemeCoOccurrenceRules; }
		}

		/// <summary>
		/// Gets the custom properties.
		/// </summary>
		/// <value>The properties.</value>
		public IDictionary<string, object> Properties
		{
			get { return _properties; }
		}
	}
}