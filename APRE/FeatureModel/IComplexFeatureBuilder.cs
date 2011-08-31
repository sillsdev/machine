using System;

namespace SIL.APRE.FeatureModel
{
	public interface IComplexFeatureBuilder
	{
		IComplexFeatureBuilder SymbolicFeature(string id, string desc, Func<ISymbolicFeatureBuilder, ISymbolicFeatureBuilder> build);
		IComplexFeatureBuilder SymbolicFeature(string id, Func<ISymbolicFeatureBuilder, ISymbolicFeatureBuilder> build);

		IComplexFeatureBuilder StringFeature(string id, string desc);
		IComplexFeatureBuilder StringFeature(string id);
		IComplexFeatureBuilder StringFeature(string id, string desc, Func<IStringFeatureBuilder, IStringFeatureBuilder> build);
		IComplexFeatureBuilder StringFeature(string id, Func<IStringFeatureBuilder, IStringFeatureBuilder> build);

		IComplexFeatureBuilder ComplexFeature(string id, string desc, Func<IComplexFeatureBuilder, IComplexFeatureBuilder> build);
		IComplexFeatureBuilder ComplexFeature(string id, Func<IComplexFeatureBuilder, IComplexFeatureBuilder> build);

		IComplexFeatureBuilder ExtantFeature(string id);

		ComplexFeature Value { get; }
	}
}
