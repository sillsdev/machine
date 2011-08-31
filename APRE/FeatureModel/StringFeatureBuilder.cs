namespace SIL.APRE.FeatureModel
{
	public class StringFeatureBuilder : IStringFeatureBuilder
	{
		private readonly StringFeature _feature;

		public StringFeatureBuilder(string id, string desc)
		{
			_feature = new StringFeature(id, desc);
		}

		public StringFeatureBuilder(string id)
		{
			_feature = new StringFeature(id);
		}

		public IStringFeatureBuilder Default(string str)
		{
			_feature.DefaultValue = new StringFeatureValue(str);
			return this;
		}

		public StringFeature Value
		{
			get { return _feature; }
		}
	}
}
