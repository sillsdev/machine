using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
	/// <summary>
	/// This class represents an allomorph of a morpheme. Allomorphs can be phonologically
	/// conditioned using environments and are applied disjunctively within a morpheme.
	/// </summary>
	public abstract class Allomorph : IComparable<Allomorph>
	{
		private readonly ObservableHashSet<AllomorphEnvironment> _requiredEnvironments;
		private readonly ObservableHashSet<AllomorphEnvironment> _excludedEnvironments;
		private readonly ObservableHashSet<AllomorphCoOccurrenceRule> _requiredAllomorphCoOccurrences;
		private readonly ObservableHashSet<AllomorphCoOccurrenceRule> _excludedAllomorphCoOccurrences; 
		private readonly Hashtable _properties;
		private readonly string _id;

		/// <summary>
		/// Initializes a new instance of the <see cref="Allomorph"/> class.
		/// </summary>
		protected Allomorph()
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
			_properties = new Hashtable();
			_id = Guid.NewGuid().ToString();
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

		internal string ID
		{
			get { return _id; }
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
		/// Gets the custom properties.
		/// </summary>
		/// <value>The properties.</value>
		public IDictionary Properties
		{
			get { return _properties; }
		}

		public bool FreeFluctuatesWith(Allomorph other)
		{
			if (this == other || Morpheme != other.Morpheme)
				return false;

			int minIndex = Math.Min(Index, other.Index);
			int maxIndex = Math.Max(Index, other.Index);
			for (int i = minIndex; i < maxIndex; i++)
			{
				Allomorph cur = Morpheme.GetAllomorph(i);
				Allomorph next = Morpheme.GetAllomorph(i + 1);
				if (!cur.ConstraintsEqual(next))
					return false;
			}
			return true;
		}

		protected virtual bool ConstraintsEqual(Allomorph other)
		{
			return _requiredEnvironments.SetEquals(other._requiredEnvironments) && _excludedEnvironments.SetEquals(other._excludedEnvironments);
		}

		internal virtual bool IsWordValid(Morpher morpher, Word word)
		{
			AllomorphEnvironment env = RequiredEnvironments.FirstOrDefault(e => !e.IsMatch(word));
			if (env != null)
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.RequiredEnvironments, this, env);
				return false;
			}

			env = ExcludedEnvironments.FirstOrDefault(e => e.IsMatch(word));
			if (env != null)
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.ExcludedEnvironments, this, env);
				return false;
			}

			AllomorphCoOccurrenceRule alloRule = RequiredAllomorphCoOccurrences.FirstOrDefault(r => !r.CoOccurs(word));
			if (alloRule != null)
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.RequiredAllomorphCoOccurrences, this, alloRule);
				return false;
			}

			alloRule = ExcludedAllomorphCoOccurrences.FirstOrDefault(r => r.CoOccurs(word));
			if (alloRule != null)
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.ExcludedAllomorphCoOccurrences, this, alloRule);
				return false;
			}

			MorphemeCoOccurrenceRule morphemeRule = Morpheme.RequiredMorphemeCoOccurrences.FirstOrDefault(r => !r.CoOccurs(word));
			if (morphemeRule != null)
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.RequiredMorphemeCoOccurrences, this, morphemeRule);
				return false;
			}

			morphemeRule = Morpheme.ExcludedMorphemeCoOccurrences.FirstOrDefault(r => r.CoOccurs(word));
			if (morphemeRule != null)
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.ExcludedMorphemeCoOccurrences, this, morphemeRule);
				return false;
			}

			return true;
		}

		public int CompareTo(Allomorph other)
		{
			if (other == null)
				return 1;

			int res = Morpheme.GetHashCode().CompareTo(other.Morpheme.GetHashCode());
			if (res != 0)
				return res;

			return Index.CompareTo(other.Index);
		}
	}
}
