namespace SIL.Machine.FeatureModel.Fluent
{
	public class StringFeatureBuilder : IStringFeatureSyntax
	{
		private readonly StringFeature _feature;

		public StringFeatureBuilder(string id, string desc)
		{
			_feature = new StringFeature(id) {Description = desc};
		}

		public StringFeatureBuilder(string id)
		{
			_feature = new StringFeature(id);
		}

		public IStringFeatureSyntax Default(string str)
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
