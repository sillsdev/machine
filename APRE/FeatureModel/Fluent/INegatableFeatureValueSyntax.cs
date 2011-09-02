namespace SIL.APRE.FeatureModel.Fluent
{
	public interface INegatableFeatureValueSyntax
	{
		IFeatureStructSyntax EqualTo(string string1, params string[] strings);
		IFeatureStructSyntax EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols);
		IFeatureStructSyntax EqualToAny { get; }
		IFeatureStructSyntax EqualToVariable(string name);
	}
}
