using System;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IDisjunctiveFeatureStructSyntax
	{
		IDisjunctiveFeatureValueSyntax Feature(string featureID);
		IDisjunctiveFeatureValueSyntax Feature(Feature feature);
		IDisjunctiveFeatureStructSyntax Symbol(string symbolID1, params string[] symbolIDs);
		IDisjunctiveFeatureStructSyntax Symbol(int id, string symbolID1, params string[] symbolIDs);
		IDisjunctiveFeatureStructSyntax Symbol(FeatureSymbol symbol1, params FeatureSymbol[] symbols);
		IDisjunctiveFeatureStructSyntax Symbol(int id, FeatureSymbol symbol1, params FeatureSymbol[] symbols);

		IDisjunctiveFeatureStructSyntax And(Func<IFirstDisjunctSyntax, IFinalDisjunctSyntax> build);

		FeatureStruct Value { get; }
	}
}
