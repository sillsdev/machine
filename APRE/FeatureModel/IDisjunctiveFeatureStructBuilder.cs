using System;

namespace SIL.APRE.FeatureModel
{
	public interface IDisjunctiveFeatureStructBuilder
	{
		IDisjunctiveFeatureValueBuilder Feature(string featureID);
		IDisjunctiveFeatureValueBuilder Feature(Feature feature);
		IDisjunctiveFeatureStructBuilder Symbol(string symbolID1, params string[] symbolIDs);
		IDisjunctiveFeatureStructBuilder Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols);

		IDisjunctiveFeatureStructBuilder And(Func<IFirstDisjunctBuilder, IFinalDisjunctBuilder> build);

		FeatureStruct Value { get; }
	}
}
