namespace SIL.APRE.FeatureModel
{
	public class StringFeatureBuilder
	{
		public static implicit operator StringFeature(StringFeatureBuilder builder)
		{
			return builder.ToStringFeature();
		}

		private readonly StringFeature _feature;

		public StringFeatureBuilder(string id, string desc)
		{
			_feature = new StringFeature(id, desc);
		}

		public StringFeatureBuilder(string id)
		{
			_feature = new StringFeature(id);
		}

		public StringFeatureBuilder Default(string str)
		{
			_feature.DefaultValue = new StringFeatureValue(str);
			return this;
		}

		public StringFeature ToStringFeature()
		{
			return _feature;
		}
	}
}
