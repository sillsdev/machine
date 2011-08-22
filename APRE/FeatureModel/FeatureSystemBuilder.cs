using System;

namespace SIL.APRE.FeatureModel
{
	public class FeatureSystemBuilder
	{
		public static implicit operator FeatureSystem(FeatureSystemBuilder builder)
		{
			return builder.ToFeatureSystem();
		}

		private readonly FeatureSystem _featSys;

		public FeatureSystemBuilder()
		{
			_featSys = new FeatureSystem();
		}

		public FeatureSystemBuilder SymbolicFeature(string id, string desc, Action<SymbolicFeatureBuilder> build)
		{
			var featureBuilder = new SymbolicFeatureBuilder(id, desc);
			_featSys.AddFeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public FeatureSystemBuilder SymbolicFeature(string id, Action<SymbolicFeatureBuilder> build)
		{
			var featureBuilder = new SymbolicFeatureBuilder(id);
			_featSys.AddFeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public FeatureSystemBuilder StringFeature(string id, string desc)
		{
			Feature feature = new StringFeature(id, desc);
			_featSys.AddFeature(feature);
			return this;
		}

		public FeatureSystemBuilder StringFeature(string id)
		{
			Feature feature = new StringFeature(id);
			_featSys.AddFeature(feature);
			return this;
		}

		public FeatureSystemBuilder StringFeature(string id, string desc, Action<StringFeatureBuilder> build)
		{
			var featureBuilder = new StringFeatureBuilder(id, desc);
			_featSys.AddFeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public FeatureSystemBuilder StringFeature(string id, Action<StringFeatureBuilder> build)
		{
			var featureBuilder = new StringFeatureBuilder(id);
			_featSys.AddFeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public FeatureSystemBuilder ComplexFeature(string id, string desc, Action<ComplexFeatureBuilder> build)
		{
			var featureBuilder = new ComplexFeatureBuilder(_featSys, id, desc);
			_featSys.AddFeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public FeatureSystemBuilder ComplexFeature(string id, Action<ComplexFeatureBuilder> build)
		{
			var featureBuilder = new ComplexFeatureBuilder(_featSys, id);
			_featSys.AddFeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public FeatureSystemBuilder ExtantFeature(string id)
		{
			_featSys.AddFeature(_featSys.GetFeature(id));
			return this;
		}

		public FeatureSystem ToFeatureSystem()
		{
			return _featSys;
		}
	}
}
