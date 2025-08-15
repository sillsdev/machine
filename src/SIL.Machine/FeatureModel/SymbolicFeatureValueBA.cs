using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.FeatureModel
{
    // This class uses BitArray instead of ulong for processing symbol feature values
    // to overcome the 64 value limit that ulong has.
    public class SymbolicFeatureValueBA : SymbolicFeatureValue
    {
        public static implicit operator SymbolicFeatureValueBA(FeatureSymbol symbol)
        {
            return new SymbolicFeatureValueBA(symbol);
        }

        public static explicit operator FeatureSymbol(SymbolicFeatureValueBA sfv)
        {
            return sfv._first;
        }

        private BitArray _flagsBA = new BitArray(sizeof(ulong) * 8, false);
        private FeatureSymbol _first;
        private readonly int _bitArraySize;

        public SymbolicFeatureValueBA(SymbolicFeature feature)
        {
            Feature = feature;
            _bitArraySize = Feature.PossibleSymbols.Count;
            _flagsBA = new BitArray(_bitArraySize, false);
        }

        public SymbolicFeatureValueBA(IEnumerable<FeatureSymbol> values)
        {
            FeatureSymbol[] symbols = values.ToArray();
            if (symbols.Length == 0)
                throw new ArgumentException("values cannot be empty", "values");
            Feature = symbols[0].Feature;
            _first = symbols[0];
            _bitArraySize = symbols[0].Feature.PossibleSymbols.Count;
            _flagsBA = new BitArray(_bitArraySize, false);
            Set(symbols);
        }

        public SymbolicFeatureValueBA(FeatureSymbol value)
        {
            Feature = value.Feature;
            _bitArraySize = Feature.PossibleSymbols.Count;
            _flagsBA = new BitArray(_bitArraySize, false);
            _first = value;
            Set(value.ToEnumerable());
        }

        public SymbolicFeatureValueBA(SymbolicFeature feature, string varName, bool agree)
            : base(varName, agree)
        {
            Feature = feature;
            _bitArraySize = Feature.PossibleSymbols.Count;
            _flagsBA = new BitArray(_bitArraySize, false);
        }

        private SymbolicFeatureValueBA(SymbolicFeatureValueBA sfv)
            : base(sfv)
        {
            Feature = sfv.Feature;
            _first = sfv._first;
            _flagsBA = new BitArray(sfv._flagsBA);
        }

        private SymbolicFeatureValueBA(SymbolicFeature feature, BitArray flagsBA)
        {
            Feature = feature;
            _flagsBA = new BitArray(flagsBA);
            SetFirst();
        }

        private void Set(IEnumerable<FeatureSymbol> symbols)
        {
            BitArray maskBA = new BitArray(_bitArraySize);
            foreach (FeatureSymbol symbol in symbols)
            {
                maskBA.Set(symbol.Index, true);
                _flagsBA.Or(maskBA);
            }
        }

        public override IEnumerable<FeatureSymbol> Values
        {
            get { return Feature.PossibleSymbols.Where(Get); }
        }

        private void SetFirst()
        {
            _first = HasAnySet(_flagsBA) ? Feature.PossibleSymbols.First(Get) : null;
        }

        protected override bool Get(FeatureSymbol symbol)
        {
            return _flagsBA.Get(symbol.Index);
        }

        // TODO: replace with C# version when available (starting with v.8)
        private bool HasAnySet(BitArray mask)
        {
            foreach (bool flag in mask)
            {
                if (flag)
                    return true;
            }
            return false;
        }

        protected override bool IsSatisfiable
        {
            get { return IsVariable || HasAnySet(_flagsBA); }
        }

        protected override bool IsUninstantiated
        {
            get { return !IsVariable && BitArraysAreEqual(_flagsBA, Feature.MaskBA); }
        }

        private bool BitArraysAreEqual(BitArray array1, BitArray array2)
        {
            return array1.Cast<bool>().SequenceEqual(array2.Cast<bool>());
        }

        protected override bool IsSupersetOf(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValueBA otherSfv))
                return false;

            // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
            BitArray flags = new BitArray(_flagsBA);
            BitArray otherFlags = new BitArray(otherSfv._flagsBA);
            if (!not && !notOther)
            {
                return BitArraysAreEqual(flags.And(otherFlags), otherSfv._flagsBA);
            }
            else
            {
                BitArray notOtherFlags = new BitArray(otherFlags).Not();
                BitArray notOtherFlagsAndFeatureMask = new BitArray(notOtherFlags.And(Feature.MaskBA));
                if (!not)
                {
                    return BitArraysAreEqual(flags.And(notOtherFlagsAndFeatureMask), notOtherFlagsAndFeatureMask);
                }
                else
                {
                    BitArray notFlags = new BitArray(_flagsBA).Not();
                    BitArray notFlagsAndFeatureMask = new BitArray(notFlags.And(Feature.MaskBA));
                    if (!notOther)
                    {
                        return BitArraysAreEqual(notFlagsAndFeatureMask.And(otherSfv._flagsBA), otherSfv._flagsBA);
                    }
                    else
                    {
                        return BitArraysAreEqual(
                            notFlagsAndFeatureMask.And(notOtherFlagsAndFeatureMask),
                            notOtherFlagsAndFeatureMask
                        );
                    }
                }
            }
        }

        protected override bool Overlaps(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValueBA otherSfv))
            {
                return false;
            }

            // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
            BitArray flags = new BitArray(_flagsBA);
            BitArray otherFlags = new BitArray(otherSfv._flagsBA);
            if (!not && !notOther)
            {
                return HasAnySet(flags.And(otherFlags));
            }
            else
            {
                BitArray notOtherFlags = new BitArray(otherFlags).Not();
                BitArray notOtherFlagsAndFeatureMask = new BitArray(notOtherFlags.And(Feature.MaskBA));
                if (!not)
                {
                    return HasAnySet(flags.And(notOtherFlagsAndFeatureMask));
                }
                else
                {
                    BitArray notFlags = new BitArray(_flagsBA).Not();
                    BitArray notFlagsAndFeatureMask = new BitArray(notFlags.And(Feature.MaskBA));
                    if (!notOther)
                    {
                        return HasAnySet(notFlagsAndFeatureMask.And(otherSfv._flagsBA));
                    }
                    else
                    {
                        return HasAnySet((notFlagsAndFeatureMask).And(notOtherFlagsAndFeatureMask));
                    }
                }
            }
        }

        protected override void IntersectWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValueBA otherSfv))
                return;

            // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
            BitArray otherFlags = new BitArray(otherSfv._flagsBA);
            BitArray flags = new BitArray(_flagsBA);
            if (!not && !notOther)
            {
                _flagsBA = new BitArray(_flagsBA.And(otherSfv._flagsBA));
            }
            else
            {
                BitArray notOtherFlags = ((BitArray)otherFlags.Clone()).Not();
                BitArray notOtherFlagsAndFeatureMask = new BitArray(notOtherFlags.And(Feature.MaskBA));
                if (!not)
                {
                    _flagsBA = new BitArray(_flagsBA.And(notOtherFlagsAndFeatureMask));
                }
                else
                {
                    BitArray notFlags = ((BitArray)flags.Clone()).Not();
                    BitArray notFlagsAndFeatureMask = new BitArray(notFlags.And(Feature.MaskBA));
                    if (!notOther)
                    {
                        _flagsBA = new BitArray(notFlagsAndFeatureMask.And(otherSfv._flagsBA));
                    }
                    else
                    {
                        _flagsBA = new BitArray(notFlagsAndFeatureMask.And(notOtherFlagsAndFeatureMask));
                    }
                }
            }
            SetFirst();
        }

        protected override void UnionWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValueBA otherSfv))
                return;

            // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
            BitArray flags = new BitArray(_flagsBA);
            BitArray otherFlags = new BitArray(otherSfv._flagsBA);
            if (!not && !notOther)
            {
                _flagsBA = new BitArray(flags.Or(otherFlags));
            }
            else
            {
                BitArray notOtherFlags = ((BitArray)otherFlags.Clone()).Not();
                BitArray notOtherFlagsAndFeatureMask = new BitArray(notOtherFlags.And(Feature.MaskBA));
                if (!not)
                {
                    _flagsBA = new BitArray(_flagsBA.Or(notOtherFlagsAndFeatureMask));
                }
                else
                {
                    BitArray notFlags = ((BitArray)flags.Clone()).Not();
                    BitArray notFlagsAndFeatureMask = new BitArray(notFlags.And(Feature.MaskBA));
                    if (!notOther)
                    {
                        _flagsBA = new BitArray(notFlagsAndFeatureMask.Or(otherFlags));
                    }
                    else
                    {
                        _flagsBA = new BitArray(notFlagsAndFeatureMask.Or(notOtherFlagsAndFeatureMask));
                    }
                }
            }
            SetFirst();
        }

        protected override void ExceptWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValueBA otherSfv))
                return;

            // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
            BitArray otherFlags = new BitArray(otherSfv._flagsBA);
            BitArray notOtherFlags = otherFlags.Not();
            BitArray notOtherFlagsAndFeatureMask = new BitArray(notOtherFlags.And(Feature.MaskBA));
            if (!not && !notOther)
            {
                _flagsBA.And(notOtherFlagsAndFeatureMask);
            }
            else if (!not)
            {
                _flagsBA.And(otherSfv._flagsBA);
            }
            else
            {
                BitArray flags = new BitArray(_flagsBA);
                BitArray notFlags = ((BitArray)flags.Clone()).Not();
                BitArray notFlagsAndFeatureMask = new BitArray(notFlags.And(Feature.MaskBA));
                if (!notOther)
                {
                    _flagsBA = notFlagsAndFeatureMask.And(notOtherFlagsAndFeatureMask);
                }
                else
                {
                    _flagsBA = notFlagsAndFeatureMask.And(otherSfv._flagsBA);
                }
            }
        }

        protected override SimpleFeatureValue CloneImpl()
        {
            return Clone();
        }

        public override SimpleFeatureValue Negation()
        {
            // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
            BitArray flags = new BitArray(_flagsBA);
            BitArray notFlagsAndFeatureMask = flags.Not().And(Feature.MaskBA);
            return IsVariable
                ? new SymbolicFeatureValueBA(Feature, VariableName, !Agree)
                : new SymbolicFeatureValueBA(Feature, notFlagsAndFeatureMask);
        }

        public override bool ValueEquals(SimpleFeatureValue other)
        {
            return other is SymbolicFeatureValueBA otherSfv && ValueEquals(otherSfv);
        }

        public bool ValueEquals(SymbolicFeatureValueBA other)
        {
            if (other == null)
                return false;

            return base.ValueEquals(other) && BitArraysAreEqual(_flagsBA, other._flagsBA);
        }

        protected override int GetValuesHashCode()
        {
            int code = base.GetValuesHashCode();
            return code * 31 + GetHashCode(_flagsBA);
        }

        public int GetHashCode(BitArray array)
        {
            int hash = 0;
            foreach (bool value in array)
            {
                hash ^= (value ? 2 : 1);
            }
            return hash;
        }

        public new SymbolicFeatureValueBA Clone()
        {
            return new SymbolicFeatureValueBA(this);
        }
    }
}
