namespace SIL.Machine.FeatureModel.Fluent
{
	public interface ISymbolicFeatureSyntax
	{
		IDefaultSymbolicFeatureSyntax Symbol(string id, string desc);
		IDefaultSymbolicFeatureSyntax Symbol(string id);

		SymbolicFeature Value { get; }
	}
}
