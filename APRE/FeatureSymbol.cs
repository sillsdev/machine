namespace SIL.APRE
{
	/// <summary>
	/// This class represents a feature value.
	/// </summary>
	public class FeatureSymbol : IDBearer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureSymbol"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="description">The description.</param>
		public FeatureSymbol(string id, string description)
			: base(id, description)
		{
			SymbolIndex = -1;
		}

		public FeatureSymbol(string id)
			: this(id, id)
		{
		}

		/// <summary>
		/// Gets or sets the feature.
		/// </summary>
		/// <value>The feature.</value>
		public Feature Feature { get; internal set; }

		/// <summary>
		/// Gets or sets the index of this value in feature bundles.
		/// </summary>
		/// <value>The index in feature bundles.</value>
		internal int SymbolIndex { get; set; }
	}
}
