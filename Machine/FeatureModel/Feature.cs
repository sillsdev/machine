namespace SIL.Machine.FeatureModel
{
	/// <summary>
	/// This class represents a feature.
	/// </summary>
	public abstract class Feature : IDBearerBase
	{
		protected Feature(string id)
			: base(id)
		{
		}

		/// <summary>
		/// Gets all default values.
		/// </summary>
		/// <value>The default values.</value>
		public FeatureValue DefaultValue { get; set; }

		public double Weight { get; set; }
	}
}
