using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SIL.Collections;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents an allomorph of a morpheme. Allomorphs can be phonologically
	/// conditioned using environments and are applied disjunctively within a morpheme.
	/// </summary>
	public abstract class Allomorph : IDBearerBase, IComparable<Allomorph>
	{
		private readonly ObservableHashSet<AllomorphEnvironment> _requiredEnvironments;
		private readonly ObservableHashSet<AllomorphEnvironment> _excludedEnvironments;
		private readonly ObservableHashSet<AllomorphCoOccurrenceRule> _requiredAllomorphCoOccurrences;
		private readonly ObservableHashSet<AllomorphCoOccurrenceRule> _excludedAllomorphCoOccurrences; 
		private readonly Dictionary<string, string> _properties;

		/// <summary>
		/// Initializes a new instance of the <see cref="Allomorph"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		protected Allomorph(string id)
			: base(id)
		{
			Index = -1;
			_requiredEnvironments = new ObservableHashSet<AllomorphEnvironment>();
			_requiredEnvironments.CollectionChanged += EnvironmentsChanged;
			_excludedEnvironments = new ObservableHashSet<AllomorphEnvironment>();
			_excludedEnvironments.CollectionChanged += EnvironmentsChanged;
			_requiredAllomorphCoOccurrences = new ObservableHashSet<AllomorphCoOccurrenceRule>();
			_requiredAllomorphCoOccurrences.CollectionChanged += AllomorphCoOccurrencesChanged;
			_excludedAllomorphCoOccurrences = new ObservableHashSet<AllomorphCoOccurrenceRule>();
			_excludedAllomorphCoOccurrences.CollectionChanged += AllomorphCoOccurrencesChanged;
			_properties = new Dictionary<string, string>();
		}

		private void AllomorphCoOccurrencesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (AllomorphCoOccurrenceRule cooccur in e.OldItems)
					cooccur.Key = null;
			}
			if (e.NewItems != null)
			{
				foreach (AllomorphCoOccurrenceRule cooccur in e.NewItems)
					cooccur.Key = this;
			}
		}

		private void EnvironmentsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (AllomorphEnvironment env in e.OldItems)
					env.Allomorph = null;
			}
			if (e.NewItems != null)
			{
				foreach (AllomorphEnvironment env in e.NewItems)
					env.Allomorph = this;
			}
		}

		/// <summary>
		/// Gets or sets the morpheme.
		/// </summary>
		/// <value>The morpheme.</value>
		public Morpheme Morpheme { get; internal set; }

		/// <summary>
		/// Gets or sets the index of this allomorph in the morpheme.
		/// </summary>
		/// <value>The index.</value>
		public int Index { get; internal set; }

		/// <summary>
		/// Gets or sets the required environments.
		/// </summary>
		/// <value>The required environments.</value>
		public ICollection<AllomorphEnvironment> RequiredEnvironments
		{
			get { return _requiredEnvironments; }
		}

		/// <summary>
		/// Gets or sets the excluded environments.
		/// </summary>
		/// <value>The excluded environments.</value>
		public ICollection<AllomorphEnvironment> ExcludedEnvironments
		{
			get { return _excludedEnvironments; }
		}

		/// <summary>
		/// Gets or sets the required allomorph co-occurrences.
		/// </summary>
		/// <value>The required allomorph co-occurrences.</value>
		public ICollection<AllomorphCoOccurrenceRule> RequiredAllomorphCoOccurrences
		{
			get { return _requiredAllomorphCoOccurrences; }
		}

		/// <summary>
		/// Gets or sets the excluded allomorph co-occurrences.
		/// </summary>
		/// <value>The excluded allomorph co-occurrences.</value>
		public ICollection<AllomorphCoOccurrenceRule> ExcludedAllomorphCoOccurrences
		{
			get { return _excludedAllomorphCoOccurrences; }
		}

		/// <summary>
		/// Gets or sets the properties.
		/// </summary>
		/// <value>The properties.</value>
		public IDictionary<string, string> Properties
		{
			get { return _properties; }
		}

		public virtual bool ConstraintsEqual(Allomorph other)
		{
			return _requiredEnvironments.SetEquals(other._requiredEnvironments) && _excludedEnvironments.SetEquals(other._excludedEnvironments);
		}

		public int CompareTo(Allomorph other)
		{
			if (other == null)
				return 1;

			int res = string.CompareOrdinal(Morpheme.ID, other.Morpheme.ID);
			if (res != 0)
				return res;

			return Index.CompareTo(other.Index);
		}
	}
}
