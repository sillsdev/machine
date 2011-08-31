using System;

namespace SIL.APRE.FeatureModel
{
	public class FeatureSystemBuilder : IFeatureSystemBuilder
	{
		private readonly FeatureSystem _featSys;

		public FeatureSystemBuilder()
		{
			_featSys = new FeatureSystem();
		}

		public IFeatureSystemBuilder SymbolicFeature(string id, string desc, Func<ISymbolicFeatureBuilder, ISymbolicFeatureBuilder> build)
		{
			var featureBuilder = new SymbolicFeatureBuilder(id, desc);
			_featSys.AddFeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IFeatureSystemBuilder SymbolicFeature(string id, Func<ISymbolicFeatureBuilder, ISymbolicFeatureBuilder> build)
		{
			var featureBuilder = new SymbolicFeatureBuilder(id);
			_featSys.AddFeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IFeatureSystemBuilder StringFeature(string id, string desc)
		{
			Feature feature = new StringFeature(id, desc);
			_featSys.AddFeature(feature);
			return this;
		}

		public IFeatureSystemBuilder StringFeature(string id)
		{
			Feature feature = new StringFeature(id);
			_featSys.AddFeature(feature);
			return this;
		}

		public IFeatureSystemBuilder StringFeature(string id, string desc, Func<IStringFeatureBuilder, IStringFeatureBuilder> build)
		{
			var featureBuilder = new StringFeatureBuilder(id, desc);
			_featSys.AddFeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IFeatureSystemBuilder StringFeature(string id, Func<IStringFeatureBuilder, IStringFeatureBuilder> build)
		{
			var featureBuilder = new StringFeatureBuilder(id);
			_featSys.AddFeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IFeatureSystemBuilder ComplexFeature(string id, string desc, Func<IComplexFeatureBuilder, IComplexFeatureBuilder> build)
		{
			var featureBuilder = new ComplexFeatureBuilder(_featSys, id, desc);
			_featSys.AddFeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IFeatureSystemBuilder ComplexFeature(string id, Func<IComplexFeatureBuilder, IComplexFeatureBuilder> build)
		{
			var featureBuilder = new ComplexFeatureBuilder(_featSys, id);
			_featSys.AddFeature(featureBuilder.Value);
			build(featureBuilder);
			return this;
		}

		public IFeatureSystemBuilder ExtantFeature(string id)
		{
			_featSys.AddFeature(_featSys.GetFeature(id));
			return this;
		}

		public FeatureSystem Value
		{
			get { return _featSys; }
		}
	}
}
