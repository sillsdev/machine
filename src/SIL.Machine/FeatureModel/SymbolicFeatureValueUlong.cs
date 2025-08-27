using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.FeatureModel
{
    internal class SymbolicFeatureValueUlong : ISymbolicFeatureValue
    {
        public void ExceptWith(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        )
        {
            if (!not && !notOther)
                sfv.FlagsUlong = sfv.FlagsUlong & (~otherSfv.FlagsUlong & feature.MaskUlong);
            else if (!not)
                sfv.FlagsUlong = sfv.FlagsUlong & otherSfv.FlagsUlong;
            else if (!notOther)
                sfv.FlagsUlong = (~sfv.FlagsUlong & feature.MaskUlong) & (~otherSfv.FlagsUlong & feature.MaskUlong);
            else
                sfv.FlagsUlong = (~sfv.FlagsUlong & feature.MaskUlong) & otherSfv.FlagsUlong;
        }

        public bool Get(SymbolicFeatureValue sfv, FeatureSymbol symbol)
        {
            return (sfv.FlagsUlong & (1UL << symbol.Index)) != 0;
        }

        public int GetValuesHashCode(SymbolicFeatureValue sfv, int code)
        {
            return code * 31 + sfv.FlagsUlong.GetHashCode();
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
                sfv.FlagsUlong = sfv.FlagsUlong & otherSfv.FlagsUlong;
            else if (!not)
                sfv.FlagsUlong = sfv.FlagsUlong & (~otherSfv.FlagsUlong & feature.MaskUlong);
            else if (!notOther)
                sfv.FlagsUlong = (~sfv.FlagsUlong & feature.MaskUlong) & otherSfv.FlagsUlong;
            else
                sfv.FlagsUlong = (~sfv.FlagsUlong & feature.MaskUlong) & (~otherSfv.FlagsUlong & feature.MaskUlong);
        }

        public bool IsSatisfiable(SymbolicFeatureValue sfv)
        {
            return sfv.FlagsUlong != 0;
        }

        public bool IsSupersetOf(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        )
        {
            if (!not && !notOther)
                return (sfv.FlagsUlong & otherSfv.FlagsUlong) == otherSfv.FlagsUlong;
            if (!not)
            {
                return (sfv.FlagsUlong & (~otherSfv.FlagsUlong & feature.MaskUlong))
                    == (~otherSfv.FlagsUlong & feature.MaskUlong);
            }
            if (!notOther)
                return ((~sfv.FlagsUlong & feature.MaskUlong) & otherSfv.FlagsUlong) == otherSfv.FlagsUlong;
            return ((~sfv.FlagsUlong & feature.MaskUlong) & (~otherSfv.FlagsUlong & feature.MaskUlong))
                == (~otherSfv.FlagsUlong & feature.MaskUlong);
        }

        public bool IsUninstantiated(SymbolicFeatureValue sfv, SymbolicFeature feature)
        {
            return sfv.FlagsUlong == feature.MaskUlong;
        }

        public bool Overlaps(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        )
        {
            if (!not && !notOther)
                return (sfv.FlagsUlong & otherSfv.FlagsUlong) != 0;
            if (!not)
                return (sfv.FlagsUlong & (~otherSfv.FlagsUlong & feature.MaskUlong)) != 0;
            if (!notOther)
                return ((~sfv.FlagsUlong & feature.MaskUlong) & otherSfv.FlagsUlong) != 0;
            return ((~sfv.FlagsUlong & feature.MaskUlong) & (~otherSfv.FlagsUlong & feature.MaskUlong)) != 0;
        }

        public void Set(SymbolicFeatureValue sfv, IEnumerable<FeatureSymbol> symbols)
        {
            foreach (FeatureSymbol symbol in symbols)
            {
                ulong mask = 1UL << symbol.Index;
                sfv.FlagsUlong |= mask;
            }
        }

        public void SetFirst(SymbolicFeatureValue sfv, SymbolicFeature feature)
        {
            sfv.First = sfv.FlagsUlong == 0 ? null : feature.PossibleSymbols.First(sfv.Get);
        }

        public void UnionWith(
            bool not,
            SymbolicFeatureValue sfv,
            SymbolicFeatureValue otherSfv,
            bool notOther,
            SymbolicFeature feature
        )
        {
            if (!not && !notOther)
                sfv.FlagsUlong = sfv.FlagsUlong | otherSfv.FlagsUlong;
            else if (!not)
                sfv.FlagsUlong = sfv.FlagsUlong | (~otherSfv.FlagsUlong & feature.MaskUlong);
            else if (!notOther)
                sfv.FlagsUlong = (~sfv.FlagsUlong & feature.MaskUlong) | otherSfv.FlagsUlong;
            else
                sfv.FlagsUlong = (~sfv.FlagsUlong & feature.MaskUlong) | (~otherSfv.FlagsUlong & feature.MaskUlong);
        }

        public bool ValueEquals(SymbolicFeatureValue sfv, SymbolicFeatureValue otherSfv)
        {
            return sfv.FlagsUlong == otherSfv.FlagsUlong;
        }
    }
}
