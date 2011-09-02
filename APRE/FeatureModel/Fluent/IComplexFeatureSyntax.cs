using System;

namespace SIL.APRE.FeatureModel.Fluent
{
	public interface IComplexFeatureSyntax
	{
		IComplexFeatureSyntax SymbolicFeature(string id, string desc, Func<ISymbolicFeatureSyntax, ISymbolicFeatureSyntax> build);
		IComplexFeatureSyntax SymbolicFeature(string id, Func<ISymbolicFeatureSyntax, ISymbolicFeatureSyntax> build);

		IComplexFeatureSyntax StringFeature(string id, string desc);
		IComplexFeatureSyntax StringFeature(string id);
		IComplexFeatureSyntax StringFeature(string id, string desc, Func<IStringFeatureSyntax, IStringFeatureSyntax> build);
		IComplexFeatureSyntax StringFeature(string id, Func<IStringFeatureSyntax, IStringFeatureSyntax> build);

		IComplexFeatureSyntax ComplexFeature(string id, string desc, Func<IComplexFeatureSyntax, IComplexFeatureSyntax> build);
		IComplexFeatureSyntax ComplexFeature(string id, Func<IComplexFeatureSyntax, IComplexFeatureSyntax> build);

		IComplexFeatureSyntax ExtantFeature(string id);

		ComplexFeature Value { get; }
	}
}
