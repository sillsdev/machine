using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab
{
	/// <summary>
	/// This class represents an allomorph of a morpheme. Allomorphs can be phonologically
	/// conditioned using environments and are applied disjunctively within a morpheme.
	/// </summary>
	public abstract class Allomorph : IComparable<Allomorph>
	{
		private readonly HashSet<AllomorphEnvironment> _environments;
		private readonly HashSet<AllomorphCoOccurrenceRule> _allomorphCoOccurrenceRules;
		private readonly Properties _properties;
		private readonly string _id;

		/// <summary>
		/// Initializes a new instance of the <see cref="Allomorph"/> class.
		/// </summary>
		protected Allomorph()
		{
			Index = -1;
			_environments = new HashSet<AllomorphEnvironment>();
			_allomorphCoOccurrenceRules = new HashSet<AllomorphCoOccurrenceRule>();
			_properties = new Properties();
			_id = Guid.NewGuid().ToString();
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
			if (this == other)
				return true;

			if (Morpheme != other.Morpheme)
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

		internal bool IsWordValid(Morpher morpher, Word word)
		{
			return IsWordValid(morpher, this, word);
		}

		protected virtual bool IsWordValid(Morpher morpher, Allomorph allomorph, Word word)
		{
			AllomorphEnvironment env = Environments.FirstOrDefault(e => !e.IsWordValid(allomorph, word));
			if (env != null)
			{
				if (morpher != null && morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.Environments, this, env);
				return false;
			}

			AllomorphCoOccurrenceRule alloRule = AllomorphCoOccurrenceRules.FirstOrDefault(r => !r.IsWordValid(allomorph, word));
			if (alloRule != null)
			{
				if (morpher != null && morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.AllomorphCoOccurrenceRules, this, alloRule);
				return false;
			}

			MorphemeCoOccurrenceRule morphemeRule = Morpheme.MorphemeCoOccurrenceRules.FirstOrDefault(r => !r.IsWordValid(Morpheme, word));
			if (morphemeRule != null)
			{
				if (morpher != null && morpher.TraceManager.IsTracing)
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.MorphemeCoOccurrenceRules, this, morphemeRule);
				return false;
			}

			if (allomorph == this)
			{
				foreach (int i in word.GetDisjunctiveAllomorphApplications(allomorph) ?? Enumerable.Range(0, Index))
				{
					Allomorph prevAllomorph = Morpheme.GetAllomorph(i);
					if (!FreeFluctuatesWith(prevAllomorph) && prevAllomorph.IsWordValid(null, allomorph, word))
					{
						if (morpher.TraceManager.IsTracing)
							morpher.TraceManager.Failed(morpher.Language, word, FailureReason.DisjunctiveAllomorph, this, prevAllomorph);
						return false;
					}
				}
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
