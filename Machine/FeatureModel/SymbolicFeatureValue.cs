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
			return sfv._values.First();
		}

		private readonly SymbolicFeature _feature;
		private IDBearerSet<FeatureSymbol> _values;

		public SymbolicFeatureValue(SymbolicFeature feature)
		{
			_feature = feature;
			_values = new IDBearerSet<FeatureSymbol>();
		}

		public SymbolicFeatureValue(IEnumerable<FeatureSymbol> values)
		{
			_values = new IDBearerSet<FeatureSymbol>(values);
			if (_values.Count == 0)
				throw new ArgumentException("values cannot be empty", "values");
			_feature = _values.First().Feature;
		}

		public SymbolicFeatureValue(FeatureSymbol value)
		{
			_feature = value.Feature;
			_values = new IDBearerSet<FeatureSymbol> {value};
		}

		public SymbolicFeatureValue(SymbolicFeature feature, string varName, bool agree)
			: base(varName, agree)
		{
			_feature = feature;
			_values = new IDBearerSet<FeatureSymbol>();
		}

		protected SymbolicFeatureValue(SymbolicFeatureValue sfv)
			: base(sfv)
		{
			_feature = sfv._feature;
			_values = new IDBearerSet<FeatureSymbol>(sfv._values);
		}

		public IEnumerable<FeatureSymbol> Values
		{
			get { return _values; }
		}

		public SymbolicFeature Feature
		{
			get { return _feature; }
		}

		protected override bool IsSatisfiable
		{
			get { return base.IsSatisfiable || _values.Count > 0; }
		}

		protected override bool IsUninstantiated
		{
			get { return base.IsUninstantiated && _values.SetEquals(_feature.PossibleSymbols); }
		}

		public bool Contains(FeatureSymbol symbol)
		{
			return _values.Contains(symbol);
		}

		public bool Contains(string symbolID)
		{
			return _values.Contains(symbolID);
		}

		public bool Overlaps(IEnumerable<FeatureSymbol> symbols)
		{
			return _values.Overlaps(symbols);
		}

		public bool Overlaps(IEnumerable<string> ids)
		{
			return _values.Overlaps(ids);
		}

		protected override bool Overlaps(bool not, SimpleFeatureValue other, bool notOther)
		{
			var otherSfv = other as SymbolicFeatureValue;
			if (otherSfv == null)
				return false;

			if (!not && !notOther)
				return _values.Overlaps(otherSfv._values);
			if (!not)
				return !_values.IsSubsetOf(otherSfv._values);
			if (!notOther)
				return !_values.IsSupersetOf(otherSfv._values);
			return !_values.IsSupersetOf(_feature.PossibleSymbols.Except(otherSfv._values));
		}

		protected override void IntersectWith(bool not, SimpleFeatureValue other, bool notOther)
		{
			var otherSfv = other as SymbolicFeatureValue;
			if (otherSfv == null)
				return;

			if (!not && !notOther)
				_values.IntersectWith(otherSfv._values);
			else if (!not)
				_values.ExceptWith(otherSfv._values);
			else if (!notOther)
				_values = new IDBearerSet<FeatureSymbol>(otherSfv._values.Except(_values));
			else
				_values = new IDBearerSet<FeatureSymbol>(_feature.PossibleSymbols.Except(_values.Union(otherSfv._values)));
		}

		protected override void UnionWith(bool not, SimpleFeatureValue other, bool notOther)
		{
			var otherSfv = other as SymbolicFeatureValue;
			if (otherSfv == null)
				return;

			if (!not && !notOther)
				_values.UnionWith(otherSfv._values);
			else if (!not)
				_values = new IDBearerSet<FeatureSymbol>(_feature.PossibleSymbols.Except(otherSfv._values.Except(_values)));
			else if (!notOther)
				_values = new IDBearerSet<FeatureSymbol>(_feature.PossibleSymbols.Except(_values.Except(otherSfv._values)));
			else
				_values = new IDBearerSet<FeatureSymbol>(_feature.PossibleSymbols.Except(_values.Intersect(otherSfv._values)));
		}

		protected override void ExceptWith(bool not, SimpleFeatureValue other, bool notOther)
		{
			var otherSfv = other as SymbolicFeatureValue;
			if (otherSfv == null)
				return;

			if (!not && !notOther)
				_values.ExceptWith(otherSfv._values);
			else if (!not)
				_values.IntersectWith(otherSfv._values);
			else if (!notOther)
				_values = new IDBearerSet<FeatureSymbol>(_feature.PossibleSymbols.Except(_values.Union(otherSfv._values)));
			else
				_values = new IDBearerSet<FeatureSymbol>(otherSfv._values.Except(_values));
		}

		protected override SimpleFeatureValue DeepCloneImpl()
		{
			return DeepClone();
		}

		public override SimpleFeatureValue Negation()
		{
			return IsVariable ? new SymbolicFeatureValue(_feature, VariableName, !Agree) : new SymbolicFeatureValue(_feature.PossibleSymbols.Except(_values));
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

			return base.ValueEquals(other) && _values.SetEquals(other._values);
		}

		protected override int GetValuesHashCode()
		{
			int code = base.GetValuesHashCode();
			return code * 31 + _values.OrderBy(sym => sym.ID).GetSequenceHashCode();
		}

		public new SymbolicFeatureValue DeepClone()
		{
			return new SymbolicFeatureValue(this);
		}

		public override string ToString()
		{
			var sb = new StringBuilder(base.ToString());
			if (_values.Count == 1)
			{
				sb.Append(_values.First());
			}
			else if (_values.Count > 1)
			{
				bool firstValue = true;
				sb.Append("{");
				foreach (FeatureSymbol value in _values)
				{
					if (!firstValue)
						sb.Append(", ");
					sb.Append(value.ToString());
					firstValue = false;
				}
				sb.Append("}");
			}
			return sb.ToString();
		}
	}
}
