using System.Collections.Generic;

namespace SIL.Machine.FeatureModel.Fluent
{
    public interface INegatableFeatureValueSyntax
    {
        IFeatureStructSyntax EqualTo(params string[] strings);
        IFeatureStructSyntax EqualTo(IEnumerable<string> strings);
        IFeatureStructSyntax EqualTo(int id, params string[] strings);
        IFeatureStructSyntax EqualTo(int id, IEnumerable<string> strings);
        IFeatureStructSyntax EqualTo(params FeatureSymbol[] symbols);
        IFeatureStructSyntax EqualTo(IEnumerable<FeatureSymbol> symbols);
        IFeatureStructSyntax EqualTo(int id, params FeatureSymbol[] symbols);
        IFeatureStructSyntax EqualTo(int id, IEnumerable<FeatureSymbol> symbols);
        IFeatureStructSyntax EqualToVariable(string name);
        IFeatureStructSyntax EqualToVariable(int id, string name);
    }
}
