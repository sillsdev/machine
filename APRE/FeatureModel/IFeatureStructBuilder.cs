namespace SIL.APRE.FeatureModel
{
	public interface IFeatureStructBuilder
	{
		IFeatureValueBuilder Feature(string featureID);
		IFeatureValueBuilder Feature(Feature feature);
		IFeatureStructBuilder Symbol(string symbolID1, params string[] symbolIDs);
		IFeatureStructBuilder Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols);

		FeatureStruct Value { get; }
	}
}
