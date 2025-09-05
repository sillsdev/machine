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

        private static ISymbolicFeatureValueFlags CreateFlags(SymbolicFeature feature)
        {
            if (feature.PossibleSymbols.Count <= MaxUlongSize)
                return new UlongSymbolicFeatureValueFlags(feature);
            return new BitArraySymbolicFeatureValueFlags(feature);
        }

        internal static void ForceBitArrayFlagsImplementation()
        {
            MaxUlongSize = 0;
        }

        internal static void ResetFlagsImplementation()
        {
            MaxUlongSize = sizeof(ulong) * 8;
        }

        private static int MaxUlongSize { get; set; } = sizeof(ulong) * 8;

        private readonly SymbolicFeature _feature;
        private readonly ISymbolicFeatureValueFlags _flags;
        private FeatureSymbol _first;

        public SymbolicFeatureValue(SymbolicFeature feature)
        {
            _feature = feature;
            _flags = CreateFlags(_feature);
        }

        public SymbolicFeatureValue(IEnumerable<FeatureSymbol> values)
        {
            FeatureSymbol[] symbols = values.ToArray();
            if (symbols.Length == 0)
                throw new ArgumentException("values cannot be empty", "values");
            _feature = symbols[0].Feature;
            _flags = CreateFlags(_feature);
            _flags.Set(symbols);
            _first = symbols[0];
        }

        public SymbolicFeatureValue(FeatureSymbol value)
        {
            _feature = value.Feature;
            _flags = CreateFlags(_feature);
            _flags.Set(value.ToEnumerable());
            _first = value;
        }

        public SymbolicFeatureValue(SymbolicFeature feature, string varName, bool agree)
            : base(varName, agree)
        {
            _feature = feature;
            _flags = CreateFlags(_feature);
        }

        private SymbolicFeatureValue(SymbolicFeatureValue sfv)
            : base(sfv)
        {
            _feature = sfv._feature;
            _flags = sfv._flags.Clone();
            _first = sfv._first;
        }

        private SymbolicFeatureValue(SymbolicFeature feature, ISymbolicFeatureValueFlags flags)
        {
            _feature = feature;
            _flags = flags;
            SetFirst();
        }

        public IEnumerable<FeatureSymbol> Values
        {
            get { return _feature.PossibleSymbols.Where(_flags.Get); }
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
            _first = _flags.GetFirst();
        }

        public SymbolicFeature Feature
        {
            get { return _feature; }
        }

        protected override bool IsSatisfiable
        {
            get { return base.IsSatisfiable || _flags.HasAnySet(); }
        }

        protected override bool IsUninstantiated
        {
            get { return base.IsUninstantiated && _flags.HasAllSet(); }
        }

        protected override bool IsSupersetOf(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return false;

            return _flags.IsSupersetOf(not, otherSfv._flags, notOther);
        }

        protected override bool Overlaps(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return false;

            return _flags.Overlaps(not, otherSfv._flags, notOther);
        }

        protected override void IntersectWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return;

            _flags.IntersectWith(not, otherSfv._flags, notOther);
            SetFirst();
        }

        protected override void UnionWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return;

            _flags.UnionWith(not, otherSfv._flags, notOther);
            SetFirst();
        }

        protected override void ExceptWith(bool not, SimpleFeatureValue other, bool notOther)
        {
            if (!(other is SymbolicFeatureValue otherSfv))
                return;

            _flags.ExceptWith(not, otherSfv._flags, notOther);
        }

        protected override SimpleFeatureValue CloneImpl()
        {
            return Clone();
        }

        public override SimpleFeatureValue Negation()
        {
            return IsVariable
                ? new SymbolicFeatureValue(_feature, VariableName, !Agree)
                : new SymbolicFeatureValue(_feature, _flags.Not());
        }

        public override bool ValueEquals(SimpleFeatureValue other)
        {
            return other is SymbolicFeatureValue otherSfv && ValueEquals(otherSfv);
        }

        public bool ValueEquals(SymbolicFeatureValue other)
        {
            if (other == null)
                return false;

            return base.ValueEquals(other) && _flags.ValueEquals(other._flags);
        }

        protected override int GetValuesHashCode()
        {
            int code = base.GetValuesHashCode();
            return code * 31 + _flags.GetValuesHashCode();
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
