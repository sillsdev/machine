using System;

namespace SIL.APRE.FeatureModel
{
	public class ComplexFeatureBuilder : IComplexFeatureBuilder
	{
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

		public IComplexFeatureBuilder SymbolicFeature(string id, string desc, Func<ISymbolicFeatureBuilder, ISymbolicFeatureBuilder> build)
		{
			var featureBuilder = new SymbolicFeatureBuilder(id, desc);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureBuilder SymbolicFeature(string id, Func<ISymbolicFeatureBuilder, ISymbolicFeatureBuilder> build)
		{
			var featureBuilder = new SymbolicFeatureBuilder(id);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureBuilder StringFeature(string id, string desc)
		{
			Feature feature = new StringFeature(id, desc);
			_feature.AddSubfeature(feature);
			return this;
		}

		public IComplexFeatureBuilder StringFeature(string id)
		{
			Feature feature = new StringFeature(id);
			_feature.AddSubfeature(feature);
			return this;
		}

		public IComplexFeatureBuilder StringFeature(string id, string desc, Func<IStringFeatureBuilder, IStringFeatureBuilder> build)
		{
			var featureBuilder = new StringFeatureBuilder(id, desc);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureBuilder StringFeature(string id, Func<IStringFeatureBuilder, IStringFeatureBuilder> build)
		{
			var featureBuilder = new StringFeatureBuilder(id);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureBuilder ComplexFeature(string id, string desc, Func<IComplexFeatureBuilder, IComplexFeatureBuilder> build)
		{
			var featureBuilder = new ComplexFeatureBuilder(_featSys, id, desc);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureBuilder ComplexFeature(string id, Func<IComplexFeatureBuilder, IComplexFeatureBuilder> build)
		{
			var featureBuilder = new ComplexFeatureBuilder(_featSys, id);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureBuilder ExtantFeature(string id)
		{
			_feature.AddSubfeature(_featSys.GetFeature(id));
			return this;
		}

		public ComplexFeature Value
		{
			get { return _feature; }
		}
	}
}
