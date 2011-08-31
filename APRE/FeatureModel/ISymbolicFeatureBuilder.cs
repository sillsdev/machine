namespace SIL.APRE.FeatureModel
{
	public interface ISymbolicFeatureBuilder
	{
		IDefaultableSymbolicFeatureBuilder Symbol(string id, string desc);
		IDefaultableSymbolicFeatureBuilder Symbol(string id);

		SymbolicFeature Value { get; }
	}
}
