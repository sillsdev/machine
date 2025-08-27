using System.Collections.Generic;

namespace SIL.Machine.FeatureModel
{
    internal interface ISymbolicFeatureValue
    {
        void ExceptWith(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        );
        bool Get(SymbolicFeatureValue sfv, FeatureSymbol symbol);
        int GetValuesHashCode(SymbolicFeatureValue sfv, int code);
        void IntersectWith(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        );
        bool IsSatisfiable(SymbolicFeatureValue sfv);
        bool IsSupersetOf(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        );
        bool IsUninstantiated(SymbolicFeatureValue sfv, SymbolicFeature feature);
        bool Overlaps(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        );
        void Set(SymbolicFeatureValue sfv, IEnumerable<FeatureSymbol> symbols);
        void SetFirst(SymbolicFeatureValue sfv, SymbolicFeature feature);
        void UnionWith(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        );
        bool ValueEquals(SymbolicFeatureValue sfv, SymbolicFeatureValue otherSFv);
    }
}
