using System;
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
            return sfv._first;
        }

        public static FeatureSymbol GetFeatureSymbolFromFeatureStruct(FeatureStruct fs, SymbolicFeature sf)
        {
            var value = fs.GetValue(sf);
            FeatureSymbol fSym =
                (value is SymbolicFeatureValueBA ba)
                    ? fSym = (FeatureSymbol)(SymbolicFeatureValueBA)value
                    : fSym = (FeatureSymbol)value;
            return fSym;
        }

        protected SymbolicFeature _feature;
        private ulong _flags;
        private FeatureSymbol _first;

        public SymbolicFeatureValue() { }

        public SymbolicFeatureValue(SymbolicFeature feature)
        {
            Feature = feature;
        }

        public SymbolicFeatureValue(IEnumerable<FeatureSymbol> values)
        {
            FeatureSymbol[] symbols = values.ToArray();
            if (symbols.Length == 0)
                throw new ArgumentException("values cannot be empty", "values");
            Feature = symbols[0].Feature;
            _first = symbols[0];
            Set(symbols);
        }

        public SymbolicFeatureValue(FeatureSymbol value)
        {
            Feature = value.Feature;
            _first = value;
            Set(value.ToEnumerable());
        }

        public SymbolicFeatureValue(SymbolicFeature feature, string varName, bool agree)
            : base(varName, agree)
        {
            Feature = feature;
        }

        protected SymbolicFeatureValue(SymbolicFeatureValue sfv)
            : base(sfv)
        {
            Feature = sfv.Feature;
            _first = sfv._first;
            _flags = sfv._flags;
        }

        protected SymbolicFeatureValue(SymbolicFeature feature, ulong flags)
        {
            Feature = feature;
            _flags = flags;
            SetFirst();
        }

        public SymbolicFeatureValue(string varName, bool agree)
            : base(varName, agree) { }

        private void Set(IEnumerable<FeatureSymbol> symbols)
        {
            foreach (FeatureSymbol symbol in symbols)
            {
                ulong mask = 1UL << symbol.Index;
                _flags |= mask;
            }
        }

        public virtual IEnumerable<FeatureSymbol> Values
        {
            get { return Feature.PossibleSymbols.Where(Get); }
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
            _first = _flags == 0 ? null : Feature.PossibleSymbols.First(Get);
        }

        protected virtual bool Get(FeatureSymbol symbol)
        {
            return (_flags & (1UL << symbol.Index)) != 0;
        }

        public SymbolicFeature Feature
        {
            get { return _feature; }
            internal set { _feature = value; }
        }

        protected override bool IsSatisfiable
        {
            get { return base.IsSatisfiable || _flags != 0; }
        }

        protected override bool IsUninstantiated
        {
            get { return base.IsUninstantiated && _flags == Feature.Mask; }
        }

        protected override bool IsSupersetOf(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return false;

            if (!not && !notOther)
            {
                return (_flags & otherSfv._flags) == otherSfv._flags;
            }
            else if (!not)
            {
                return (_flags & (~otherSfv._flags & Feature.Mask)) == (~otherSfv._flags & Feature.Mask);
            }
            else if (!notOther)
            {
                return ((~_flags & Feature.Mask) & otherSfv._flags) == otherSfv._flags;
            }
            else
            {
                return ((~_flags & Feature.Mask) & (~otherSfv._flags & Feature.Mask))
                    == (~otherSfv._flags & Feature.Mask);
            }
        }

        protected override bool Overlaps(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
            {
                return false;
            }

            if (!not && !notOther)
            {
                return (_flags & otherSfv._flags) != 0;
            }
            else if (!not)
            {
                return (_flags & (~otherSfv._flags & Feature.Mask)) != 0;
            }
            else if (!notOther)
            {
                return ((~_flags & Feature.Mask) & otherSfv._flags) != 0;
            }
            else
            {
                return ((~_flags & Feature.Mask) & (~otherSfv._flags & Feature.Mask)) != 0;
            }
        }

        protected override void IntersectWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return;

            if (!not && !notOther)
            {
                _flags = _flags & otherSfv._flags;
            }
            else if (!not)
            {
                _flags = _flags & (~otherSfv._flags & Feature.Mask);
            }
            else if (!notOther)
            {
                _flags = (~_flags & Feature.Mask) & otherSfv._flags;
            }
            else
            {
                _flags = (~_flags & Feature.Mask) & (~otherSfv._flags & Feature.Mask);
            }
            SetFirst();
        }

        protected override void UnionWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return;

            if (!not && !notOther)
            {
                _flags = _flags | otherSfv._flags;
            }
            else if (!not)
            {
                _flags = _flags | (~otherSfv._flags & Feature.Mask);
            }
            else if (!notOther)
            {
                _flags = (~_flags & Feature.Mask) | otherSfv._flags;
            }
            else
            {
                _flags = (~_flags & Feature.Mask) | (~otherSfv._flags & Feature.Mask);
            }
            SetFirst();
        }

        protected override void ExceptWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return;

            if (!not && !notOther)
            {
                _flags = _flags & (~otherSfv._flags & Feature.Mask);
            }
            else if (!not)
            {
                _flags = _flags & otherSfv._flags;
            }
            else if (!notOther)
            {
                _flags = (~_flags & Feature.Mask) & (~otherSfv._flags & Feature.Mask);
            }
            else
            {
                _flags = (~_flags & Feature.Mask) & otherSfv._flags;
            }
        }

        protected override SimpleFeatureValue CloneImpl()
        {
            return Clone();
        }

        public override SimpleFeatureValue Negation()
        {
            return IsVariable
                ? new SymbolicFeatureValue(Feature, VariableName, !Agree)
                : new SymbolicFeatureValue(Feature, ~_flags & Feature.Mask);
        }

        public override bool ValueEquals(SimpleFeatureValue other)
        {
            return other is SymbolicFeatureValue otherSfv && ValueEquals(otherSfv);
        }

        public bool ValueEquals(SymbolicFeatureValue other)
        {
            if (other == null)
                return false;

            return base.ValueEquals(other) && _flags == other._flags;
        }

        protected override int GetValuesHashCode()
        {
            int code = base.GetValuesHashCode();
            return code * 31 + _flags.GetHashCode();
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
