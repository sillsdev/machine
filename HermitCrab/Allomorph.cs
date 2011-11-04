using System;
using System.Collections.Generic;
using SIL.APRE;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents an allomorph of a morpheme. Allomorphs can be phonologically
	/// conditioned using environments and are applied disjunctively within a morpheme.
	/// </summary>
	public abstract class Allomorph : IDBearerBase, IComparable<Allomorph>
	{
		private Morpheme _morpheme;
		private IEnumerable<Environment> _requiredEnvs;
		private IEnumerable<Environment> _excludedEnvs;
		private IEnumerable<MorphCoOccurrence> _requiredAlloCoOccur;
		private IEnumerable<MorphCoOccurrence> _excludedAlloCoOccur;
		private readonly Dictionary<string, string> _properties;
		private int _index = -1;

		/// <summary>
		/// Initializes a new instance of the <see cref="Allomorph"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		protected Allomorph(string id)
			: base(id)
		{
			_properties = new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets or sets the morpheme.
		/// </summary>
		/// <value>The morpheme.</value>
		public Morpheme Morpheme
		{
			get
			{
				return _morpheme;
			}

			internal set
			{
				_morpheme = value;
			}
		}

		/// <summary>
		/// Gets or sets the index of this allomorph in the morpheme.
		/// </summary>
		/// <value>The index.</value>
		public int Index
		{
			get
			{
				return _index;
			}

			internal set
			{
				_index = value;
			}
		}

		/// <summary>
		/// Gets or sets the required environments.
		/// </summary>
		/// <value>The required environments.</value>
		public IEnumerable<Environment> RequiredEnvironments
		{
			get
			{
				return _requiredEnvs;
			}

			set
			{
				_requiredEnvs = value;
			}
		}

		/// <summary>
		/// Gets or sets the excluded environments.
		/// </summary>
		/// <value>The excluded environments.</value>
		public IEnumerable<Environment> ExcludedEnvironments
		{
			get
			{
				return _excludedEnvs;
			}

			set
			{
				_excludedEnvs = value;
			}
		}

		/// <summary>
		/// Gets or sets the required allomorph co-occurrences.
		/// </summary>
		/// <value>The required allomorph co-occurrences.</value>
		public IEnumerable<MorphCoOccurrence> RequiredAllomorphCoOccurrences
		{
			get
			{
				return _requiredAlloCoOccur;
			}

			set
			{
				_requiredAlloCoOccur = value;
			}
		}

		/// <summary>
		/// Gets or sets the excluded allomorph co-occurrences.
		/// </summary>
		/// <value>The excluded allomorph co-occurrences.</value>
		public IEnumerable<MorphCoOccurrence> ExcludedAllomorphCoOccurrences
		{
			get
			{
				return _excludedAlloCoOccur;
			}

			set
			{
				_excludedAlloCoOccur = value;
			}
		}

		/// <summary>
		/// Gets or sets the properties.
		/// </summary>
		/// <value>The properties.</value>
		public IEnumerable<KeyValuePair<string, string>> Properties
		{
			get
			{
				return _properties;
			}

			set
			{
				_properties.Clear();
				if (value != null)
				{
					foreach (KeyValuePair<string, string> kvp in value)
						_properties[kvp.Key] = kvp.Value;
				}
			}
		}

		/// <summary>
		/// Gets the property value for the specified name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The value.</returns>
		public string GetProperty(string name)
		{
			string value;
			if (_properties.TryGetValue(name, out value))
				return value;
			return null;
		}

		public int CompareTo(Allomorph other)
		{
			if (other.Morpheme != Morpheme)
				throw new ArgumentException("Cannot compare allomorphs from different morphemes.", "other");

			return _index.CompareTo(other._index);
		}
	}
}
