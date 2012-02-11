namespace SIL.Machine.FeatureModel.Fluent
{
	public interface INegatableFeatureValueSyntax
	{
		IFeatureStructSyntax EqualTo(string string1, params string[] strings);
		IFeatureStructSyntax EqualTo(int id, string string1, params string[] strings);
		IFeatureStructSyntax EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols);
		IFeatureStructSyntax EqualTo(int id, FeatureSymbol symbol1, params FeatureSymbol[] symbols);
		IFeatureStructSyntax EqualToVariable(string name);
		IFeatureStructSyntax EqualToVariable(int id, string name);
	}
}
