using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SIL.Collections;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents a morpheme. All morpheme objects should extend this class.
    /// </summary>
    public abstract class Morpheme : IDBearerBase
    {
		private readonly ObservableCollection<MorphemeCoOccurrenceRule> _requiredMorphemeCoOccurrences;
		private readonly ObservableCollection<MorphemeCoOccurrenceRule> _excludedMorphemeCoOccurrences; 

    	/// <summary>
    	/// Initializes a new instance of the <see cref="Morpheme"/> class.
    	/// </summary>
    	/// <param name="id">The id.</param>
    	protected Morpheme(string id)
            : base(id)
        {
			_requiredMorphemeCoOccurrences = new ObservableCollection<MorphemeCoOccurrenceRule>();
    		_requiredMorphemeCoOccurrences.CollectionChanged += MorphemeCoOccurrencesChanged;
			_excludedMorphemeCoOccurrences = new ObservableCollection<MorphemeCoOccurrenceRule>();
    		_excludedMorphemeCoOccurrences.CollectionChanged += MorphemeCoOccurrencesChanged;
        }

		private void MorphemeCoOccurrencesChanged(object sender, NotifyCollectionChangedEventArgs e)
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

    	/// <summary>
    	/// Gets or sets the morpheme's gloss.
    	/// </summary>
    	/// <value>The gloss.</value>
    	public string Gloss { get; set; }

    	/// <summary>
    	/// Gets or sets the required morpheme co-occurrences.
    	/// </summary>
    	/// <value>The required morpheme co-occurrences.</value>
    	public ICollection<MorphemeCoOccurrenceRule> RequiredMorphemeCoOccurrences
    	{
    		get { return _requiredMorphemeCoOccurrences; }
    	}

    	/// <summary>
    	/// Gets or sets the excluded morpheme co-occurrences.
    	/// </summary>
    	/// <value>The excluded morpheme co-occurrences.</value>
    	public ICollection<MorphemeCoOccurrenceRule> ExcludedMorphemeCoOccurrences
    	{
    		get { return _excludedMorphemeCoOccurrences; }
    	}
    }
}