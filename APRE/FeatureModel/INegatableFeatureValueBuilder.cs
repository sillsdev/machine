namespace SIL.APRE.FeatureModel
{
	public interface INegatableFeatureValueBuilder
	{
		IFeatureStructBuilder EqualTo(string string1, params string[] strings);
		IFeatureStructBuilder EqualTo(FeatureSymbol symbol1, params FeatureSymbol[] symbols);
		IFeatureStructBuilder EqualToAny { get; }
		IFeatureStructBuilder EqualToVariable(string name);
	}
}
