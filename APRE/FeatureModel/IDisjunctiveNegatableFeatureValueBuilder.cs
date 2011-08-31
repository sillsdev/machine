namespace SIL.APRE.FeatureModel
{
	public interface IDisjunctiveNegatableFeatureValueBuilder
	{
		IDisjunctiveFeatureStructBuilder EqualTo(string string1, params string[] strings);
		IDisjunctiveFeatureStructBuilder EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols);
		IDisjunctiveFeatureStructBuilder EqualToAny { get; }
		IDisjunctiveFeatureStructBuilder EqualToVariable(string name);
	}
}
