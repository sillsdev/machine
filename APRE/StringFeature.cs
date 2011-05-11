namespace SIL.APRE
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

		public override FeatureValueType ValueType
		{
			get { return FeatureValueType.String; }
		}
	}
}
