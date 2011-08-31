namespace SIL.APRE.FeatureModel
{
	public interface IStringFeatureBuilder
	{
		IStringFeatureBuilder Default(string str);
		StringFeature Value { get; }
	}
}
