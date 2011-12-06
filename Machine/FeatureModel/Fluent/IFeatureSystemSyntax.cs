using System;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IFeatureSystemSyntax
	{
		IFeatureSystemSyntax SymbolicFeature(string id, string desc, Func<ISymbolicFeatureSyntax, ISymbolicFeatureSyntax> build);
		IFeatureSystemSyntax SymbolicFeature(string id, Func<ISymbolicFeatureSyntax, ISymbolicFeatureSyntax> build);

		IFeatureSystemSyntax StringFeature(string id, string desc);
		IFeatureSystemSyntax StringFeature(string id);
		IFeatureSystemSyntax StringFeature(string id, string desc, Func<IStringFeatureSyntax, IStringFeatureSyntax> build);
		IFeatureSystemSyntax StringFeature(string id, Func<IStringFeatureSyntax, IStringFeatureSyntax> build);

		IFeatureSystemSyntax ComplexFeature(string id, string desc, Func<IComplexFeatureSyntax, IComplexFeatureSyntax> build);
		IFeatureSystemSyntax ComplexFeature(string id, Func<IComplexFeatureSyntax, IComplexFeatureSyntax> build);

		IFeatureSystemSyntax ExtantFeature(string id);

		FeatureSystem Value { get; }
	}
}
