namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IFeatureStructSyntax
	{
		IFeatureValueSyntax Feature(string featureID);
		IFeatureValueSyntax Feature(Feature feature);
		IFeatureStructSyntax Symbol(string symbolID1, params string[] symbolIDs);
		IFeatureStructSyntax Symbol(int id, string symbolID1, params string[] symbolIDs);
		IFeatureStructSyntax Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols);
		IFeatureStructSyntax Symbol(int id, FeatureSymbol symbol1, params FeatureSymbol[] symbols);

		FeatureStruct Value { get; }
	}
}
