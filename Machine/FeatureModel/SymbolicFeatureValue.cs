using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Collections;

namespace SIL.Machine.FeatureModel
{
	public class SymbolicFeatureValue : SimpleFeatureValue, IDeepCloneable<SymbolicFeatureValue>
	{
		public static implicit operator SymbolicFeatureValue(FeatureSymbol symbol)
		{
			return new SymbolicFeatureValue(symbol);
		}

		public static explicit operator FeatureSymbol(SymbolicFeatureValue sfv)
		{
			return sfv._first;
		}

		private readonly SymbolicFeature _feature;
		private ulong _flags;
		private FeatureSymbol _first;

		public SymbolicFeatureValue(SymbolicFeature feature)
		{
			_feature = feature;
		}

		public SymbolicFeatureValue(IEnumerable<FeatureSymbol> values)
		{
			FeatureSymbol[] symbols = values.ToArray();
			if (symbols.Length == 0)
				throw new ArgumentException("values cannot be empty", "values");
			_feature = symbols[0].Feature;
			_first = symbols[0];
			Set(symbols);
		}

		public SymbolicFeatureValue(FeatureSymbol value)
		{
			_feature = value.Feature;
			_first = value;
			Set(value.ToEnumerable());
		}

		public SymbolicFeatureValue(SymbolicFeature feature, string varName, bool agree)
			: base(varName, agree)
		{
			_feature = feature;
		}

		private SymbolicFeatureValue(SymbolicFeatureValue sfv)
			: base(sfv)
		{
			_feature = sfv._feature;
			_first = sfv._first;
			_flags = sfv._flags;
		}

		private SymbolicFeatureValue(SymbolicFeature feature, ulong flags)
		{
			_feature = feature;
			_flags = flags;
			SetFirst();
		}

		private void Set(IEnumerable<FeatureSymbol> symbols)
		{
			foreach (FeatureSymbol symbol in symbols)
			{
				ulong mask = 1UL << symbol.Index;
				_flags |= mask;
			}
		}

		public IEnumerable<FeatureSymbol> Values
		{
			get
			{
				return _feature.PossibleSymbols.Where(Get);
			}
		}

		private void SetFirst()
		{
			_first = _flags == 0 ? null : _feature.PossibleSymbols.First(Get);
		}

		private bool Get(FeatureSymbol symbol)
		{
			return (_flags & (1UL << symbol.Index)) != 0;
		}

		public SymbolicFeature Feature
		{
			get { return _feature; }
		}

		protected override bool IsSatisfiable
		{
			get { return base.IsSatisfiable || _flags != 0; }
		}

		protected override bool IsUninstantiated
		{
			get { return base.IsUninstantiated && _flags == _feature.Mask; }
		}

		protected override bool IsSupersetOf(bool not, SimpleFeatureValue other, bool notOther)
		{
			var otherSfv = other as SymbolicFeatureValue;
			if (otherSfv == null)
				return false;

			if (!not && !notOther)
				return (_flags & otherSfv._flags) == otherSfv._flags;
			if (!not)
				return (_flags & (~otherSfv._flags & _feature.Mask)) == (~otherSfv._flags & _feature.Mask);
			if (!notOther)
				return ((~_flags & _feature.Mask) & otherSfv._flags) == otherSfv._flags;
			return ((~_flags & _feature.Mask) & (~otherSfv._flags & _feature.Mask)) == (~otherSfv._flags & _feature.Mask);
		}

		protected override bool Overlaps(bool not, SimpleFeatureValue other, bool notOther)
		{
			var otherSfv = other as SymbolicFeatureValue;
			if (otherSfv == null)
				return false;

			if (!not && !notOther)
				return (_flags & otherSfv._flags) != 0;
			if (!not)
				return (_flags & (~otherSfv._flags & _feature.Mask)) != 0;
			if (!notOther)
				return ((~_flags & _feature.Mask) & otherSfv._flags) != 0;
			return ((~_flags & _feature.Mask) & (~otherSfv._flags & _feature.Mask)) != 0;
		}

		protected override void IntersectWith(bool not, SimpleFeatureValue other, bool notOther)
		{
			var otherSfv = other as SymbolicFeatureValue;
			if (otherSfv == null)
				return;

			if (!not && !notOther)
				_flags = _flags & otherSfv._flags;
			else if (!not)
				_flags = _flags & (~otherSfv._flags & _feature.Mask);
			else if (!notOther)
				_flags = (~_flags & _feature.Mask) & otherSfv._flags;
			else
				_flags = (~_flags & _feature.Mask) & (~otherSfv._flags & _feature.Mask);
			SetFirst();
		}

		protected override void UnionWith(bool not, SimpleFeatureValue other, bool notOther)
		{
			var otherSfv = other as SymbolicFeatureValue;
			if (otherSfv == null)
				return;

			if (!not && !notOther)
				_flags = _flags | otherSfv._flags;
			else if (!not)
				_flags = _flags | (~otherSfv._flags & _feature.Mask);
			else if (!notOther)
				_flags = (~_flags & _feature.Mask) | otherSfv._flags;
			else
				_flags = (~_flags & _feature.Mask) | (~otherSfv._flags & _feature.Mask);
			SetFirst();
		}

		protected override void ExceptWith(bool not, SimpleFeatureValue other, bool notOther)
		{
			var otherSfv = other as SymbolicFeatureValue;
			if (otherSfv == null)
				return;

			if (!not && !notOther)
				_flags = _flags & (~otherSfv._flags & _feature.Mask);
			else if (!not)
				_flags = _flags & otherSfv._flags;
			else if (!notOther)
				_flags = (~_flags & _feature.Mask) & (~otherSfv._flags & _feature.Mask);
			else
				_flags = (~_flags & _feature.Mask) & otherSfv._flags;
		}

		protected override SimpleFeatureValue DeepCloneImpl()
		{
			return DeepClone();
		}

		public override SimpleFeatureValue Negation()
		{
			return IsVariable ? new SymbolicFeatureValue(_feature, VariableName, !Agree) : new SymbolicFeatureValue(_feature, (~_flags & _feature.Mask));
		}

		public override bool ValueEquals(SimpleFeatureValue other)
		{
			var otherSfv = other as SymbolicFeatureValue;
			return otherSfv != null && ValueEquals(otherSfv);
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

		public new SymbolicFeatureValue DeepClone()
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
