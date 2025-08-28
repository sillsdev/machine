using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.FeatureModel
{
    internal class SymbolicFeatureValueBitArray : ISymbolicFeatureValue
    {
        public void ExceptWith(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        )
        {
            // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
            BitArray notOtherFlagsAndFeatureMask = new BitArray(
                new BitArray(otherSfv.FlagsBitArray).Not().And(feature.MaskBitArray)
            );
            if (!not && !notOther)
            {
                sfv.FlagsBitArray.And(notOtherFlagsAndFeatureMask);
            }
            else if (!not)
            {
                sfv.FlagsBitArray.And(otherSfv.FlagsBitArray);
            }
            else
            {
                BitArray notFlagsAndFeatureMask = new BitArray(
                    new BitArray(sfv.FlagsBitArray).Not().And(feature.MaskBitArray)
                );
                if (!notOther)
                {
                    sfv.FlagsBitArray = notFlagsAndFeatureMask.And(notOtherFlagsAndFeatureMask);
                }
                else
                {
                    sfv.FlagsBitArray = notFlagsAndFeatureMask.And(otherSfv.FlagsBitArray);
                }
            }
        }

        public bool Get(SymbolicFeatureValue sfv, FeatureSymbol symbol)
        {
            return sfv.FlagsBitArray.Get(symbol.Index);
        }

        public int GetValuesHashCode(SymbolicFeatureValue sfv, int code)
        {
            return code * 31 + GetHashCode(sfv.FlagsBitArray);
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

        public bool IsSatisfiable(SymbolicFeatureValue sfv)
        {
            return HasAnySet(sfv.FlagsBitArray);
        }

        public bool IsUninstantiated(SymbolicFeatureValue sfv, SymbolicFeature feature)
        {
            return BitArraysAreEqual(sfv.FlagsBitArray, feature.MaskBitArray);
        }

        public void Set(SymbolicFeatureValue sfv, IEnumerable<FeatureSymbol> symbols)
        {
            BitArray maskBA = new BitArray(sfv.BitArraySize);
            foreach (FeatureSymbol symbol in symbols)
            {
                maskBA.Set(symbol.Index, true);
                sfv.FlagsBitArray.Or(maskBA);
            }
        }

        public void SetFirst(SymbolicFeatureValue sfv, SymbolicFeature feature)
        {
            sfv.First = HasAnySet(sfv.FlagsBitArray) ? feature.PossibleSymbols.First(sfv.Get) : null;
        }

        public void IntersectWith(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        )
        {
            if (!not && !notOther)
            {
                sfv.FlagsBitArray = new BitArray(sfv.FlagsBitArray.And(otherSfv.FlagsBitArray));
            }
            else
            {
                // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
                BitArray notOtherFlagsAndFeatureMask = new BitArray(
                    new BitArray(otherSfv.FlagsBitArray).Not().And(feature.MaskBitArray)
                );
                if (!not)
                {
                    sfv.FlagsBitArray = new BitArray(sfv.FlagsBitArray.And(notOtherFlagsAndFeatureMask));
                }
                else
                {
                    BitArray notFlagsAndFeatureMask = new BitArray(
                        new BitArray(sfv.FlagsBitArray).Not().And(feature.MaskBitArray)
                    );
                    if (!notOther)
                    {
                        sfv.FlagsBitArray = new BitArray(notFlagsAndFeatureMask.And(otherSfv.FlagsBitArray));
                    }
                    else
                    {
                        sfv.FlagsBitArray = new BitArray(notFlagsAndFeatureMask.And(notOtherFlagsAndFeatureMask));
                    }
                }
            }
        }

        public bool IsSupersetOf(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        )
        {
            // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
            BitArray flags = new BitArray(sfv.FlagsBitArray);
            BitArray otherFlags = new BitArray(otherSfv.FlagsBitArray);
            if (!not && !notOther)
            {
                return BitArraysAreEqual(flags.And(otherFlags), otherSfv.FlagsBitArray);
            }
            else
            {
                BitArray notOtherFlagsAndFeatureMask = new BitArray(
                    new BitArray(otherFlags).Not().And(feature.MaskBitArray)
                );
                if (!not)
                {
                    return BitArraysAreEqual(flags.And(notOtherFlagsAndFeatureMask), notOtherFlagsAndFeatureMask);
                }
                else
                {
                    BitArray notFlags = new BitArray(sfv.FlagsBitArray).Not();
                    BitArray notFlagsAndFeatureMask = new BitArray(notFlags.And(feature.MaskBitArray));
                    if (!notOther)
                    {
                        return BitArraysAreEqual(
                            notFlagsAndFeatureMask.And(otherSfv.FlagsBitArray),
                            otherSfv.FlagsBitArray
                        );
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

        public bool Overlaps(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        )
        {
            // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
            BitArray flags = new BitArray(sfv.FlagsBitArray);
            BitArray otherFlags = new BitArray(otherSfv.FlagsBitArray);
            if (!not && !notOther)
            {
                return HasAnySet(flags.And(otherFlags));
            }
            else
            {
                BitArray notOtherFlags = new BitArray(otherFlags).Not();
                BitArray notOtherFlagsAndFeatureMask = new BitArray(notOtherFlags.And(feature.MaskBitArray));
                if (!not)
                {
                    return HasAnySet(flags.And(notOtherFlagsAndFeatureMask));
                }
                else
                {
                    BitArray notFlags = new BitArray(sfv.FlagsBitArray).Not();
                    BitArray notFlagsAndFeatureMask = new BitArray(notFlags.And(feature.MaskBitArray));
                    if (!notOther)
                    {
                        return HasAnySet(notFlagsAndFeatureMask.And(otherSfv.FlagsBitArray));
                    }
                    else
                    {
                        return HasAnySet((notFlagsAndFeatureMask).And(notOtherFlagsAndFeatureMask));
                    }
                }
            }
        }

        public void UnionWith(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        )
        {
            // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
            BitArray flags = new BitArray(sfv.FlagsBitArray);
            BitArray otherFlags = new BitArray(otherSfv.FlagsBitArray);
            if (!not && !notOther)
            {
                sfv.FlagsBitArray = new BitArray(flags.Or(otherFlags));
            }
            else
            {
                BitArray notOtherFlagsAndFeatureMask = new BitArray(
                    new BitArray(otherFlags).Not().And(feature.MaskBitArray)
                );
                if (!not)
                {
                    sfv.FlagsBitArray = new BitArray(sfv.FlagsBitArray.Or(notOtherFlagsAndFeatureMask));
                }
                else
                {
                    BitArray notFlagsAndFeatureMask = new BitArray(new BitArray(flags).Not().And(feature.MaskBitArray));
                    if (!notOther)
                    {
                        sfv.FlagsBitArray = new BitArray(notFlagsAndFeatureMask.Or(otherFlags));
                    }
                    else
                    {
                        sfv.FlagsBitArray = new BitArray(notFlagsAndFeatureMask.Or(notOtherFlagsAndFeatureMask));
                    }
                }
            }
        }

        public bool ValueEquals(SymbolicFeatureValue sfv, SymbolicFeatureValue otherSfv)
        {
            return BitArraysAreEqual(sfv.FlagsBitArray, otherSfv.FlagsBitArray);
        }

        private bool BitArraysAreEqual(BitArray array1, BitArray array2)
        {
            return array1.Cast<bool>().SequenceEqual(array2.Cast<bool>());
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
    }
}
