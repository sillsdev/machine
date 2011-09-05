namespace SIL.APRE.FeatureModel.Fluent
{
	public interface IDisjunctiveNegatableFeatureValueSyntax
	{
		IDisjunctiveFeatureStructSyntax EqualTo(string string1, params string[] strings);
		IDisjunctiveFeatureStructSyntax EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols);
		IDisjunctiveFeatureStructSyntax EqualToVariable(string name);
	}
}
