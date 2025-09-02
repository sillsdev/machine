using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.FeatureModel
{
    internal class UlongSymbolicFeatureValueFlags : ISymbolicFeatureValueFlags
    {
        private readonly SymbolicFeature _feature;
        private readonly ulong _mask;
        private ulong _flags = 0;

        public UlongSymbolicFeatureValueFlags(SymbolicFeature feature)
        {
            _feature = feature;
            _mask = (1UL << feature.PossibleSymbols.Count) - 1UL;
        }

        private UlongSymbolicFeatureValueFlags(SymbolicFeature feature, ulong mask, ulong flags)
        {
            _feature = feature;
            _mask = mask;
            _flags = flags;
        }

        public bool HasAnySet()
        {
            return _flags != 0;
        }

        public bool HasAllSet()
        {
            return _flags == _mask;
        }

        public bool Get(FeatureSymbol symbol)
        {
            return (_flags & (1UL << symbol.Index)) != 0;
        }

        public FeatureSymbol GetFirst()
        {
            return _flags == 0 ? null : _feature.PossibleSymbols.First(Get);
        }

        public void Set(IEnumerable<FeatureSymbol> symbols)
        {
            foreach (FeatureSymbol symbol in symbols)
            {
                ulong mask = 1UL << symbol.Index;
                _flags |= mask;
            }
        }

        public bool IsSupersetOf(bool not, ISymbolicFeatureValueFlags other, bool notOther)
        {
            var otherUlong = (UlongSymbolicFeatureValueFlags)other;
            if (!not && !notOther)
                return (_flags & otherUlong._flags) == otherUlong._flags;
            if (!not)
                return (_flags & (~otherUlong._flags & _mask)) == (~otherUlong._flags & _mask);
            if (!notOther)
                return ((~_flags & _mask) & otherUlong._flags) == otherUlong._flags;
            return ((~_flags & _mask) & (~otherUlong._flags & _mask)) == (~otherUlong._flags & _mask);
        }

        public bool Overlaps(bool not, ISymbolicFeatureValueFlags other, bool notOther)
        {
            var otherUlong = (UlongSymbolicFeatureValueFlags)other;
            if (!not && !notOther)
                return (_flags & otherUlong._flags) != 0;
            if (!not)
                return (_flags & (~otherUlong._flags & _mask)) != 0;
            if (!notOther)
                return ((~_flags & _mask) & otherUlong._flags) != 0;
            return ((~_flags & _mask) & (~otherUlong._flags & _mask)) != 0;
        }

        public void IntersectWith(bool not, ISymbolicFeatureValueFlags other, bool notOther)
        {
            var otherUlong = (UlongSymbolicFeatureValueFlags)other;
            if (!not && !notOther)
                _flags = _flags & otherUlong._flags;
            else if (!not)
                _flags = _flags & (~otherUlong._flags & _mask);
            else if (!notOther)
                _flags = (~_flags & _mask) & otherUlong._flags;
            else
                _flags = (~_flags & _mask) & (~otherUlong._flags & _mask);
        }

        public void UnionWith(bool not, ISymbolicFeatureValueFlags other, bool notOther)
        {
            var otherUlong = (UlongSymbolicFeatureValueFlags)other;
            if (!not && !notOther)
                _flags = _flags | otherUlong._flags;
            else if (!not)
                _flags = _flags | (~otherUlong._flags & _mask);
            else if (!notOther)
                _flags = (~_flags & _mask) | otherUlong._flags;
            else
                _flags = (~_flags & _mask) | (~otherUlong._flags & _mask);
        }

        public void ExceptWith(bool not, ISymbolicFeatureValueFlags other, bool notOther)
        {
            var otherUlong = (UlongSymbolicFeatureValueFlags)other;
            if (!not && !notOther)
                _flags = _flags & (~otherUlong._flags & _mask);
            else if (!not)
                _flags = _flags & otherUlong._flags;
            else if (!notOther)
                _flags = (~_flags & _mask) & (~otherUlong._flags & _mask);
            else
                _flags = (~_flags & _mask) & otherUlong._flags;
        }

        public ISymbolicFeatureValueFlags Not()
        {
            return new UlongSymbolicFeatureValueFlags(_feature, _mask, ~_flags & _mask);
        }

        public bool ValueEquals(ISymbolicFeatureValueFlags other)
        {
            var otherUlong = (UlongSymbolicFeatureValueFlags)other;
            return _flags == otherUlong._flags;
        }

        public int GetValuesHashCode()
        {
            return _flags.GetHashCode();
        }

        public ISymbolicFeatureValueFlags Clone()
        {
            return new UlongSymbolicFeatureValueFlags(_feature, _mask, _flags);
        }
    }
}
