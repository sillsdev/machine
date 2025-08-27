using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.FeatureModel
{
    public class SymbolicFeatureValue : SimpleFeatureValue, ICloneable<SymbolicFeatureValue>
    {
        public static implicit operator SymbolicFeatureValue(FeatureSymbol symbol)
        {
            return new SymbolicFeatureValue(symbol);
        }

        public static explicit operator FeatureSymbol(SymbolicFeatureValue sfv)
        {
            return sfv.First;
        }

        private SymbolicFeatureValueUlong _sfvUlong;
        private SymbolicFeatureValueBitArray _sfvBitArray;
        private readonly SymbolicFeature _feature;
        private ulong _flagsUlong;
        private FeatureSymbol _first;
        private BitArray _flagsBitArray = null;
        private int _bitArraySize = 0;

        // this can be set to 0 for testing the BitArray code
        public static int NeedToUseBitArray { get; set; } = sizeof(ulong) * 8;

        // To test using the BitArray method, comment the preceding line and uncomment the following line
        //public static int NeedToUseBitArray { get; set; } = 0;

        public SymbolicFeatureValue(SymbolicFeature feature)
        {
            MakeValueItemToUse(feature.PossibleSymbols.Count);
            _feature = feature;
        }

        public SymbolicFeatureValue(IEnumerable<FeatureSymbol> values)
        {
            FeatureSymbol[] symbols = values.ToArray();
            if (symbols.Length == 0)
                throw new ArgumentException("values cannot be empty", "values");
            _feature = symbols[0].Feature;
            MakeValueItemToUse(_feature.PossibleSymbols.Count);
            First = symbols[0];
            Set(symbols);
        }

        public SymbolicFeatureValue(FeatureSymbol value)
        {
            _feature = value.Feature;
            MakeValueItemToUse(_feature.PossibleSymbols.Count);
            First = value;
            Set(value.ToEnumerable());
        }

        public SymbolicFeatureValue(SymbolicFeature feature, string varName, bool agree)
            : base(varName, agree)
        {
            MakeValueItemToUse(feature.PossibleSymbols.Count);
            _feature = feature;
        }

        private SymbolicFeatureValue(SymbolicFeatureValue sfv)
            : base(sfv)
        {
            _feature = sfv._feature;
            MakeValueItemToUse(_feature.PossibleSymbols.Count);
            First = sfv.First;
            FlagsUlong = sfv.FlagsUlong;
            if (sfv.FlagsBitArray != null)
                FlagsBitArray = new BitArray(sfv.FlagsBitArray);
        }

        private SymbolicFeatureValue(SymbolicFeature feature, ulong flags)
        {
            MakeValueItemToUse(feature.PossibleSymbols.Count);
            _feature = feature;
            FlagsUlong = flags;
            SetFirst();
        }

        public SymbolicFeatureValue(SymbolicFeature feature, BitArray notFlagsAndFeatureMask)
            : this(feature)
        {
            FlagsBitArray = new BitArray(notFlagsAndFeatureMask);
        }

        private void MakeValueItemToUse(int size)
        {
            BitArraySize = size;
            if (BitArraySize <= NeedToUseBitArray)
            {
                SfvUlong = new SymbolicFeatureValueUlong();
            }
            else
            {
                _flagsBitArray = new BitArray(BitArraySize, false);
                SfvBitArray = new SymbolicFeatureValueBitArray();
            }
        }

        private void Set(IEnumerable<FeatureSymbol> symbols)
        {
            if (SfvUlong != null)
                SfvUlong.Set(this, symbols);
            else
                SfvBitArray.Set(this, symbols);
        }

        public IEnumerable<FeatureSymbol> Values
        {
            get { return _feature.PossibleSymbols.Where(Get); }
        }

        public bool IsSupersetOf(SymbolicFeatureValue other, bool notOther = false)
        {
            return IsSupersetOf(false, other, notOther);
        }

        public bool Overlaps(SymbolicFeatureValue other, bool notOther = false)
        {
            return Overlaps(false, other, notOther);
        }

        private void SetFirst()
        {
            if (SfvUlong != null)
                SfvUlong.SetFirst(this, _feature);
            else
                SfvBitArray.SetFirst(this, _feature);
        }

        public bool Get(FeatureSymbol symbol)
        {
            if (SfvUlong != null)
                return SfvUlong.Get(this, symbol);
            else
                return SfvBitArray.Get(this, symbol);
        }

        public SymbolicFeature Feature
        {
            get { return _feature; }
        }

        protected override bool IsSatisfiable
        {
            get
            {
                bool sfvValue = false;
                if (SfvUlong != null)
                    sfvValue = SfvUlong.IsSatisfiable(this);
                else
                    sfvValue = SfvBitArray.IsSatisfiable(this);
                return base.IsSatisfiable || sfvValue;
            }
        }

        protected override bool IsUninstantiated
        {
            get
            {
                bool sfvValue = false;
                if (SfvUlong != null)
                    sfvValue = SfvUlong.IsUninstantiated(this, _feature);
                else
                    sfvValue = SfvBitArray.IsUninstantiated(this, _feature);
                return base.IsUninstantiated && sfvValue;
            }
        }

        public BitArray FlagsBitArray
        {
            get => _flagsBitArray;
            set => _flagsBitArray = value;
        }

        public ulong FlagsUlong
        {
            get => _flagsUlong;
            set => _flagsUlong = value;
        }
        public FeatureSymbol First
        {
            get => _first;
            set => _first = value;
        }

        internal SymbolicFeatureValueUlong SfvUlong
        {
            get => _sfvUlong;
            set => _sfvUlong = value;
        }

        internal SymbolicFeatureValueBitArray SfvBitArray
        {
            get => _sfvBitArray;
            set => _sfvBitArray = value;
        }
        public int BitArraySize
        {
            get => _bitArraySize;
            set => _bitArraySize = value;
        }

        protected override bool IsSupersetOf(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return false;

            if (SfvUlong != null)
                return SfvUlong.IsSupersetOf(not, this, otherSfv, notOther, _feature);
            else
                return SfvBitArray.IsSupersetOf(not, this, otherSfv, notOther, _feature);
        }

        protected override bool Overlaps(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return false;

            if (SfvUlong != null)
                return SfvUlong.Overlaps(not, this, otherSfv, notOther, _feature);
            else
                return SfvBitArray.Overlaps(not, this, otherSfv, notOther, _feature);
        }

        protected override void IntersectWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return;

            if (SfvUlong != null)
                SfvUlong.IntersectWith(not, this, otherSfv, notOther, _feature);
            else
                SfvBitArray.IntersectWith(not, this, otherSfv, notOther, _feature);
            SetFirst();
        }

        protected override void UnionWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return;

            if (SfvUlong != null)
                SfvUlong.UnionWith(not, this, otherSfv, notOther, _feature);
            else
                SfvBitArray.UnionWith(not, this, otherSfv, notOther, _feature);

            SetFirst();
        }

        protected override void ExceptWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return;

            if (SfvUlong != null)
                SfvUlong.ExceptWith(not, this, otherSfv, notOther, _feature);
            else
                SfvBitArray.ExceptWith(not, this, otherSfv, notOther, _feature);
        }

        protected override SimpleFeatureValue CloneImpl()
        {
            return Clone();
        }

        public override SimpleFeatureValue Negation()
        {
            if (SfvUlong != null)
            {
                return IsVariable
                    ? new SymbolicFeatureValue(_feature, VariableName, !Agree)
                    : new SymbolicFeatureValue(_feature, (~FlagsUlong & _feature.MaskUlong));
            }
            else
            {
                // Since logical operations on BitArrays change the BitArray variable, we need to create temp variables
                BitArray flags = new BitArray(_flagsBitArray);
                BitArray notFlagsAndFeatureMask = flags.Not().And(_feature.MaskBitArray);
                return IsVariable
                    ? new SymbolicFeatureValue(_feature, VariableName, !Agree)
                    : new SymbolicFeatureValue(_feature, notFlagsAndFeatureMask);
            }
        }

        public override bool ValueEquals(SimpleFeatureValue other)
        {
            return other is SymbolicFeatureValue otherSfv && ValueEquals(otherSfv);
        }

        public bool ValueEquals(SymbolicFeatureValue other)
        {
            if (other == null)
                return false;

            bool sfvValue = false;
            if (SfvUlong != null)
                sfvValue = SfvUlong.ValueEquals(this, other);
            else
                sfvValue = SfvBitArray.ValueEquals(this, other);
            return base.ValueEquals(other) && sfvValue;
        }

        protected override int GetValuesHashCode()
        {
            int code = base.GetValuesHashCode();
            if (SfvUlong != null)
                return SfvUlong.GetValuesHashCode(this, code);
            else
                return SfvBitArray.GetValuesHashCode(this, code);
        }

        public new SymbolicFeatureValue Clone()
        {
            return new SymbolicFeatureValue(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder(base.ToString());

            FeatureSymbol[] values = Values.ToArray();
            if (values.Length == 1)
            {
                sb.Append(values[0]);
            }
            else if (values.Length > 1)
            {
                bool firstValue = true;
                sb.Append("{");
                foreach (FeatureSymbol value in values.OrderBy(v => v.ToString()))
                {
                    if (!firstValue)
                        sb.Append(", ");
                    sb.Append(value);
                    firstValue = false;
                }
                sb.Append("}");
            }
            return sb.ToString();
        }
    }
}
