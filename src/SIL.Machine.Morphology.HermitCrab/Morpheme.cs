using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
	/// <summary>
	/// This class represents a morpheme. All morpheme objects should extend this class.
	/// </summary>
	public abstract class Morpheme : IMorpheme
	{
		private readonly HashSet<MorphemeCoOccurrenceRule> _morphemeCoOccurrenceRules;
		private readonly Properties _properties;

		/// <summary>
		/// Initializes a new instance of the <see cref="Morpheme"/> class.
		/// </summary>
		protected Morpheme()
		{
			_morphemeCoOccurrenceRules = new HashSet<MorphemeCoOccurrenceRule>();
			_properties = new Properties();
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

		public abstract int AllomorphCount { get; }

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