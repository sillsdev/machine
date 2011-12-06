namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IStringFeatureSyntax
	{
		IStringFeatureSyntax Default(string str);
		StringFeature Value { get; }
	}
}
