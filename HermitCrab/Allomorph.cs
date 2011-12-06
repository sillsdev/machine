using System.Collections.Generic;
using SIL.Machine;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents an allomorph of a morpheme. Allomorphs can be phonologically
	/// conditioned using environments and are applied disjunctively within a morpheme.
	/// </summary>
	public abstract class Allomorph : IDBearerBase
	{
		private readonly Dictionary<string, string> _properties;

		/// <summary>
		/// Initializes a new instance of the <see cref="Allomorph"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		protected Allomorph(string id)
			: base(id)
		{
			Index = -1;
			_properties = new Dictionary<string, string>();
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
		public IEnumerable<Environment> RequiredEnvironments { get; set; }

		/// <summary>
		/// Gets or sets the excluded environments.
		/// </summary>
		/// <value>The excluded environments.</value>
		public IEnumerable<Environment> ExcludedEnvironments { get; set; }

		/// <summary>
		/// Gets or sets the required allomorph co-occurrences.
		/// </summary>
		/// <value>The required allomorph co-occurrences.</value>
		public IEnumerable<MorphCoOccurrence> RequiredAllomorphCoOccurrences { get; set; }

		/// <summary>
		/// Gets or sets the excluded allomorph co-occurrences.
		/// </summary>
		/// <value>The excluded allomorph co-occurrences.</value>
		public IEnumerable<MorphCoOccurrence> ExcludedAllomorphCoOccurrences { get; set; }

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
	}
}
