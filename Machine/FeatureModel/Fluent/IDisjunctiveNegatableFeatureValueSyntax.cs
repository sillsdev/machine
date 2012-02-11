namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IDisjunctiveNegatableFeatureValueSyntax
	{
		IDisjunctiveFeatureStructSyntax EqualTo(string string1, params string[] strings);
		IDisjunctiveFeatureStructSyntax EqualTo(int id, string string1, params string[] strings);
		IDisjunctiveFeatureStructSyntax EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols);
		IDisjunctiveFeatureStructSyntax EqualTo(int id, FeatureSymbol symbol1, params FeatureSymbol[] symbols);
		IDisjunctiveFeatureStructSyntax EqualToVariable(string name);
		IDisjunctiveFeatureStructSyntax EqualToVariable(int id, string name);
	}
}
