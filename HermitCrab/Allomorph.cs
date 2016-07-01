using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using SIL.Collections;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents an allomorph of a morpheme. Allomorphs can be phonologically
	/// conditioned using environments and are applied disjunctively within a morpheme.
	/// </summary>
	public abstract class Allomorph : IComparable<Allomorph>
	{
		private readonly ObservableHashSet<AllomorphEnvironment> _environments;
		private readonly ObservableHashSet<AllomorphCoOccurrenceRule> _allomorphCoOccurrenceRules;
		private readonly Properties _properties;
		private readonly string _id;

		/// <summary>
		/// Initializes a new instance of the <see cref="Allomorph"/> class.
		/// </summary>
		protected Allomorph()
		{
			Index = -1;
			_environments = new ObservableHashSet<AllomorphEnvironment>();
			_environments.CollectionChanged += EnvironmentsChanged;
			_allomorphCoOccurrenceRules = new ObservableHashSet<AllomorphCoOccurrenceRule>();
			_allomorphCoOccurrenceRules.CollectionChanged += AllomorphCoOccurrenceRuleRulesChanged;
			_properties = new Properties();
			_id = Guid.NewGuid().ToString();
		}

		private void AllomorphCoOccurrenceRuleRulesChanged(object sender, NotifyCollectionChangedEventArgs e)
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
		/// Gets the environments.
		/// </summary>
		/// <value>The required environments.</value>
		public ICollection<AllomorphEnvironment> Environments
		{
			get { return _environments; }
		}

		/// <summary>
		/// Gets the allomorph co-occurrence rules.
		/// </summary>
		/// <value>The allomorph co-occurrence rules.</value>
		public ICollection<AllomorphCoOccurrenceRule> AllomorphCoOccurrenceRules
		{
			get { return _allomorphCoOccurrenceRules; }
		}

		/// <summary>
		/// Gets the custom properties.
		/// </summary>
		/// <value>The properties.</value>
		public IDictionary<string, object> Properties
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
			return _environments.SetEquals(other._environments);
		}

		internal virtual bool IsWordValid(Morpher morpher, Word word)
		{
			AllomorphEnvironment env = Environments.FirstOrDefault(e => !e.IsWordValid(word));
			if (env != null)
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.ParseFailed(morpher.Language, word, FailureReason.Environments, this, env);
				return false;
			}

			AllomorphCoOccurrenceRule alloRule = AllomorphCoOccurrenceRules.FirstOrDefault(r => !r.IsWordValid(word));
			if (alloRule != null)
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.ParseFailed(morpher.Language, word, FailureReason.AllomorphCoOccurrenceRules, this, alloRule);
				return false;
			}

			MorphemeCoOccurrenceRule morphemeRule = Morpheme.MorphemeCoOccurrenceRules.FirstOrDefault(r => !r.IsWordValid(word));
			if (morphemeRule != null)
			{
				if (morpher.TraceManager.IsTracing)
					morpher.TraceManager.ParseFailed(morpher.Language, word, FailureReason.MorphemeCoOccurrenceRules, this, morphemeRule);
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
