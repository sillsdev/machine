using System;

namespace SIL.APRE.FeatureModel
{
	public interface IFeatureSystemBuilder
	{
		IFeatureSystemBuilder SymbolicFeature(string id, string desc, Func<ISymbolicFeatureBuilder, ISymbolicFeatureBuilder> build);
		IFeatureSystemBuilder SymbolicFeature(string id, Func<ISymbolicFeatureBuilder, ISymbolicFeatureBuilder> build);

		IFeatureSystemBuilder StringFeature(string id, string desc);
		IFeatureSystemBuilder StringFeature(string id);
		IFeatureSystemBuilder StringFeature(string id, string desc, Func<IStringFeatureBuilder, IStringFeatureBuilder> build);
		IFeatureSystemBuilder StringFeature(string id, Func<IStringFeatureBuilder, IStringFeatureBuilder> build);

		IFeatureSystemBuilder ComplexFeature(string id, string desc, Func<IComplexFeatureBuilder, IComplexFeatureBuilder> build);
		IFeatureSystemBuilder ComplexFeature(string id, Func<IComplexFeatureBuilder, IComplexFeatureBuilder> build);

		IFeatureSystemBuilder ExtantFeature(string id);

		FeatureSystem Value { get; }
	}
}
