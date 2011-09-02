namespace SIL.APRE.FeatureModel
{
	public class StringFeature : Feature
	{
		public StringFeature(string id, string description)
			: base(id, description)
		{
		}

		public StringFeature(string id)
			: this(id, id)
		{
		}
	}
}
