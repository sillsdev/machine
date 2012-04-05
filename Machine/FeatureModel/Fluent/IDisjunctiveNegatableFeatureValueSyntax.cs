using System.Collections.Generic;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IDisjunctiveNegatableFeatureValueSyntax
	{
		IDisjunctiveFeatureStructSyntax EqualTo(params string[] strings);
		IDisjunctiveFeatureStructSyntax EqualTo(int id, params string[] strings);
		IDisjunctiveFeatureStructSyntax EqualTo(IEnumerable<string> strings);
		IDisjunctiveFeatureStructSyntax EqualTo(int id, IEnumerable<string> strings);
		IDisjunctiveFeatureStructSyntax EqualTo(params FeatureSymbol[] symbols);
		IDisjunctiveFeatureStructSyntax EqualTo(int id, params FeatureSymbol[] symbols);
		IDisjunctiveFeatureStructSyntax EqualTo(IEnumerable<FeatureSymbol> symbols);
		IDisjunctiveFeatureStructSyntax EqualTo(int id, IEnumerable<FeatureSymbol> symbols);
		IDisjunctiveFeatureStructSyntax EqualToVariable(string name);
		IDisjunctiveFeatureStructSyntax EqualToVariable(int id, string name);
	}
}
