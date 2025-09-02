using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.FeatureModel
{
    internal class BitArraySymbolicFeatureValueFlags : ISymbolicFeatureValueFlags
    {
        private readonly SymbolicFeature _feature;
        private readonly BitArray _flags;

        public BitArraySymbolicFeatureValueFlags(SymbolicFeature feature)
        {
            _feature = feature;
            _flags = new BitArray(feature.PossibleSymbols.Count, false);
        }

        private BitArraySymbolicFeatureValueFlags(SymbolicFeature feature, BitArray flags)
        {
            _feature = feature;
            _flags = flags;
        }

        public bool HasAnySet()
        {
            return HasAnySet(_flags);
        }

        public bool HasAllSet()
        {
            return HasAllSet(_flags);
        }

        public bool Get(FeatureSymbol symbol)
        {
            return _flags.Get(symbol.Index);
        }

        public FeatureSymbol GetFirst()
        {
            return _feature.PossibleSymbols.FirstOrDefault(Get);
        }

        public void Set(IEnumerable<FeatureSymbol> symbols)
        {
            foreach (FeatureSymbol symbol in symbols)
                _flags.Set(symbol.Index, true);
        }

        public bool IsSupersetOf(bool not, ISymbolicFeatureValueFlags other, bool notOther)
        {
            var otherBitArray = (BitArraySymbolicFeatureValueFlags)other;
            if (!not && !notOther)
            {
                return AreEqual(Copy(_flags).And(otherBitArray._flags), otherBitArray._flags);
            }
            else if (!not)
            {
                BitArray notOtherFlags = Copy(otherBitArray._flags).Not();
                return AreEqual(Copy(_flags).And(notOtherFlags), notOtherFlags);
            }
            else if (!notOther)
            {
                return AreEqual(Copy(_flags).Not().And(otherBitArray._flags), otherBitArray._flags);
            }
            else
            {
                BitArray notOtherFlags = Copy(otherBitArray._flags).Not();
                return AreEqual(Copy(_flags).Not().And(notOtherFlags), notOtherFlags);
            }
        }

        public bool Overlaps(bool not, ISymbolicFeatureValueFlags other, bool notOther)
        {
            var otherBitArray = (BitArraySymbolicFeatureValueFlags)other;
            if (!not && !notOther)
            {
                return HasAnySet(Copy(_flags).And(otherBitArray._flags));
            }
            else if (!not)
            {
                BitArray notOtherFlags = Copy(otherBitArray._flags).Not();
                return AreEqual(Copy(_flags).And(notOtherFlags), notOtherFlags);
            }
            else if (!notOther)
            {
                return AreEqual(Copy(_flags).Not().And(otherBitArray._flags), otherBitArray._flags);
            }
            else
            {
                BitArray notOtherFlags = Copy(otherBitArray._flags).Not();
                return AreEqual(Copy(_flags).Not().And(notOtherFlags), notOtherFlags);
            }
        }

        public void IntersectWith(bool not, ISymbolicFeatureValueFlags other, bool notOther)
        {
            var otherBitArray = (BitArraySymbolicFeatureValueFlags)other;
            if (!not && !notOther)
            {
                _flags.And(otherBitArray._flags);
            }
            else if (!not)
            {
                _flags.And(Copy(otherBitArray._flags).Not());
            }
            else if (!notOther)
            {
                _flags.Not().And(otherBitArray._flags);
            }
            else
            {
                _flags.Not().And(Copy(otherBitArray._flags).Not());
            }
        }

        public void UnionWith(bool not, ISymbolicFeatureValueFlags other, bool notOther)
        {
            var otherBitArray = (BitArraySymbolicFeatureValueFlags)other;
            if (!not && !notOther)
            {
                _flags.Or(otherBitArray._flags);
            }
            else if (!not)
            {
                _flags.Or(Copy(otherBitArray._flags).Not());
            }
            else if (!notOther)
            {
                _flags.Not().Or(otherBitArray._flags);
            }
            else
            {
                _flags.Not().Or(Copy(otherBitArray._flags).Not());
            }
        }

        public void ExceptWith(bool not, ISymbolicFeatureValueFlags other, bool notOther)
        {
            var otherBitArray = (BitArraySymbolicFeatureValueFlags)other;
            if (!not && !notOther)
            {
                _flags.And(Copy(otherBitArray._flags).Not());
            }
            else if (!not)
            {
                _flags.And(otherBitArray._flags);
            }
            else if (!notOther)
            {
                _flags.Not().And(Copy(otherBitArray._flags).Not());
            }
            else
            {
                _flags.Not().And(otherBitArray._flags);
            }
        }

        public ISymbolicFeatureValueFlags Not()
        {
            return new BitArraySymbolicFeatureValueFlags(_feature, Copy(_flags).Not());
        }

        public bool ValueEquals(ISymbolicFeatureValueFlags other)
        {
            var otherBitArray = (BitArraySymbolicFeatureValueFlags)other;
            return AreEqual(_flags, otherBitArray._flags);
        }

        public int GetValuesHashCode()
        {
            int hash = 0;
            foreach (bool value in _flags)
            {
                hash ^= (value ? 2 : 1);
            }
            return hash;
        }

        public ISymbolicFeatureValueFlags Clone()
        {
            return new BitArraySymbolicFeatureValueFlags(_feature, Copy(_flags));
        }

        private static bool AreEqual(BitArray array1, BitArray array2)
        {
            return array1.Cast<bool>().SequenceEqual(array2.Cast<bool>());
        }

        private static bool HasAnySet(BitArray flags)
        {
            foreach (bool flag in flags)
            {
                if (flag)
                    return true;
            }
            return false;
        }

        private static bool HasAllSet(BitArray flags)
        {
            foreach (bool flag in flags)
            {
                if (!flag)
                    return false;
            }
            return true;
        }

        private static BitArray Copy(BitArray flags)
        {
            return new BitArray(flags);
        }
    }
}
