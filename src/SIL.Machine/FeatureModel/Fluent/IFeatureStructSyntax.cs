using System.Collections.Generic;

namespace SIL.Machine.FeatureModel.Fluent
{
    public interface IFeatureStructSyntax
    {
        IFeatureValueSyntax Feature(string featureID);
        IFeatureValueSyntax Feature(Feature feature);
        IFeatureStructSyntax Symbol(params string[] symbolIDs);
        IFeatureStructSyntax Symbol(IEnumerable<string> symbolIDs);
        IFeatureStructSyntax Symbol(int id, params string[] symbolIDs);
        IFeatureStructSyntax Symbol(int id, IEnumerable<string> symbolIDs);
        IFeatureStructSyntax Symbol(params FeatureSymbol[] symbols);
        IFeatureStructSyntax Symbol(IEnumerable<FeatureSymbol> symbols);
        IFeatureStructSyntax Symbol(int id, params FeatureSymbol[] symbols);
        IFeatureStructSyntax Symbol(int id, IEnumerable<FeatureSymbol> symbols);

        FeatureStruct Value { get; }
    }
}
