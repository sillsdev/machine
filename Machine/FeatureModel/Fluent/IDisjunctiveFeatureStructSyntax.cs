using System;
using System.Collections.Generic;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IDisjunctiveFeatureStructSyntax
	{
		IDisjunctiveFeatureValueSyntax Feature(string featureID);
		IDisjunctiveFeatureValueSyntax Feature(Feature feature);
		IDisjunctiveFeatureStructSyntax Symbol(params string[] symbolIDs);
		IDisjunctiveFeatureStructSyntax Symbol(IEnumerable<string> symbolIDs);
		IDisjunctiveFeatureStructSyntax Symbol(int id, params string[] symbolIDs);
		IDisjunctiveFeatureStructSyntax Symbol(int id, IEnumerable<string> symbolIDs);
		IDisjunctiveFeatureStructSyntax Symbol(params FeatureSymbol[] symbols);
		IDisjunctiveFeatureStructSyntax Symbol(IEnumerable<FeatureSymbol> symbols);
		IDisjunctiveFeatureStructSyntax Symbol(int id, params FeatureSymbol[] symbols);
		IDisjunctiveFeatureStructSyntax Symbol(int id, IEnumerable<FeatureSymbol> symbols);

		IDisjunctiveFeatureStructSyntax And(Func<IFirstDisjunctSyntax, IFinalDisjunctSyntax> build);

		FeatureStruct Value { get; }
	}
}
