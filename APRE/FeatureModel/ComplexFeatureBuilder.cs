using System;

namespace SIL.APRE.FeatureModel
{
	public class ComplexFeatureBuilder
	{
		public static implicit operator ComplexFeature(ComplexFeatureBuilder builder)
		{
			return builder.ToComplexFeature();
		}

		private readonly FeatureSystem _featSys;
		private readonly ComplexFeature _feature;

		public ComplexFeatureBuilder(FeatureSystem featSys, string id, string desc)
		{
			_featSys = featSys;
			_feature = new ComplexFeature(id, desc);
		}

		public ComplexFeatureBuilder(FeatureSystem featSys, string id)
		{
			_featSys = featSys;
			_feature = new ComplexFeature(id);
		}

		public ComplexFeatureBuilder SymbolicFeature(string id, string desc, Action<SymbolicFeatureBuilder> build)
		{
			var featureBuilder = new SymbolicFeatureBuilder(id, desc);
			_feature.AddSubfeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public ComplexFeatureBuilder SymbolicFeature(string id, Action<SymbolicFeatureBuilder> build)
		{
			var featureBuilder = new SymbolicFeatureBuilder(id);
			_feature.AddSubfeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public ComplexFeatureBuilder StringFeature(string id, string desc)
		{
			Feature feature = new StringFeature(id, desc);
			_feature.AddSubfeature(feature);
			return this;
		}

		public ComplexFeatureBuilder StringFeature(string id)
		{
			Feature feature = new StringFeature(id);
			_feature.AddSubfeature(feature);
			return this;
		}

		public ComplexFeatureBuilder StringFeature(string id, string desc, Action<StringFeatureBuilder> build)
		{
			var featureBuilder = new StringFeatureBuilder(id, desc);
			_feature.AddSubfeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public ComplexFeatureBuilder StringFeature(string id, Action<StringFeatureBuilder> build)
		{
			var featureBuilder = new StringFeatureBuilder(id);
			_feature.AddSubfeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public ComplexFeatureBuilder ComplexFeature(string id, string desc, Action<ComplexFeatureBuilder> build)
		{
			var featureBuilder = new ComplexFeatureBuilder(_featSys, id, desc);
			_feature.AddSubfeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public ComplexFeatureBuilder ComplexFeature(string id, Action<ComplexFeatureBuilder> build)
		{
			var featureBuilder = new ComplexFeatureBuilder(_featSys, id);
			_feature.AddSubfeature(featureBuilder);
			build(featureBuilder);
			return this;
		}

		public ComplexFeatureBuilder ExtantFeature(string id)
		{
			_feature.AddSubfeature(_featSys.GetFeature(id));
			return this;
		}

		public ComplexFeature ToComplexFeature()
		{
			return _feature;
		}
	}
}
