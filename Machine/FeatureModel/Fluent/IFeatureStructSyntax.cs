namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IFeatureStructSyntax
	{
		IFeatureValueSyntax Feature(string featureID);
		IFeatureValueSyntax Feature(Feature feature);
		IFeatureStructSyntax Symbol(string symbolID1, params string[] symbolIDs);
		IFeatureStructSyntax Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols);

		FeatureStruct Value { get; }
	}
}
