using System;

namespace SIL.APRE.FeatureModel.Fluent
{
	public class ComplexFeatureBuilder : IComplexFeatureSyntax
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

		public IComplexFeatureSyntax SymbolicFeature(string id, string desc, Func<ISymbolicFeatureSyntax, ISymbolicFeatureSyntax> build)
		{
			var featureBuilder = new SymbolicFeatureBuilder(id, desc);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureSyntax SymbolicFeature(string id, Func<ISymbolicFeatureSyntax, ISymbolicFeatureSyntax> build)
		{
			var featureBuilder = new SymbolicFeatureBuilder(id);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureSyntax StringFeature(string id, string desc)
		{
			Feature feature = new StringFeature(id, desc);
			_feature.AddSubfeature(feature);
			return this;
		}

		public IComplexFeatureSyntax StringFeature(string id)
		{
			Feature feature = new StringFeature(id);
			_feature.AddSubfeature(feature);
			return this;
		}

		public IComplexFeatureSyntax StringFeature(string id, string desc, Func<IStringFeatureSyntax, IStringFeatureSyntax> build)
		{
			var featureBuilder = new StringFeatureBuilder(id, desc);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureSyntax StringFeature(string id, Func<IStringFeatureSyntax, IStringFeatureSyntax> build)
		{
			var featureBuilder = new StringFeatureBuilder(id);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureSyntax ComplexFeature(string id, string desc, Func<IComplexFeatureSyntax, IComplexFeatureSyntax> build)
		{
			var featureBuilder = new ComplexFeatureBuilder(_featSys, id, desc);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureSyntax ComplexFeature(string id, Func<IComplexFeatureSyntax, IComplexFeatureSyntax> build)
		{
			var featureBuilder = new ComplexFeatureBuilder(_featSys, id);
			_feature.AddSubfeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IComplexFeatureSyntax ExtantFeature(string id)
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
