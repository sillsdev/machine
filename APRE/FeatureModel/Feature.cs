namespace SIL.APRE.FeatureModel
{
	/// <summary>
	/// This class represents a feature.
	/// </summary>
	public abstract class Feature : IDBearerBase
	{
		protected Feature(string id, string description)
			: base(id, description)
		{
		}

		protected Feature(string id)
			: this(id, id)
		{
		}

		/// <summary>
		/// Gets all default values.
		/// </summary>
		/// <value>The default values.</value>
		public FeatureValue DefaultValue { get; set; }
	}
}
