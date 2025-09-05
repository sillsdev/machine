using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.FeatureModel
{
    internal interface ISymbolicFeatureValueFlags : ICloneable<ISymbolicFeatureValueFlags>
    {
        bool HasAnySet();
        bool HasAllSet();

        bool Get(FeatureSymbol symbol);
        FeatureSymbol GetFirst();
        void Set(IEnumerable<FeatureSymbol> symbols);

        bool IsSupersetOf(bool not, ISymbolicFeatureValueFlags other, bool notOther);
        bool Overlaps(bool not, ISymbolicFeatureValueFlags other, bool notOther);

        void IntersectWith(bool not, ISymbolicFeatureValueFlags other, bool notOther);
        void UnionWith(bool not, ISymbolicFeatureValueFlags other, bool notOther);
        void ExceptWith(bool not, ISymbolicFeatureValueFlags other, bool notOther);

        ISymbolicFeatureValueFlags Not();

        bool ValueEquals(ISymbolicFeatureValueFlags other);
        int GetValuesHashCode();
    }
}
