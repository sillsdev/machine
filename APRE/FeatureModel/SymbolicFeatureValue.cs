using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE.FeatureModel
{
	public class SymbolicFeatureValue : SimpleFeatureValue
	{
		private readonly SymbolicFeature _feature;
		private readonly IDBearerSet<FeatureSymbol> _values;

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

		public SymbolicFeatureValue(SymbolicFeatureValue sfv)
		{
			_feature = sfv._feature;
			_values = new IDBearerSet<FeatureSymbol>(sfv._values);
		}

		public IEnumerable<FeatureSymbol> Values
		{
			get { return _values; }
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

		protected override bool Overlaps(FeatureValue other, bool negate)
		{
			var otherSfv = other as SymbolicFeatureValue;
			if (otherSfv == null)
				return false;

			if (!negate)
				return _values.Overlaps(otherSfv._values);

			return _values.IsSupersetOf(otherSfv._values);
		}

		protected override void IntersectWith(FeatureValue other, bool negate)
		{
			var otherSfv = (SymbolicFeatureValue) other;
			if (!negate)
				_values.IntersectWith(otherSfv._values);
			else
				_values.ExceptWith(otherSfv._values);
		}

		protected override void UnionWith(FeatureValue other, bool negate)
		{
			var otherSfv = (SymbolicFeatureValue) other;

			_values.UnionWith(!negate ? otherSfv._values : _feature.PossibleSymbols.Except(otherSfv.Values));
		}

		public override bool Negation(out FeatureValue output)
		{
			output = new SymbolicFeatureValue(_feature.PossibleSymbols.Except(_values));
			return true;
		}

		public override string ToString()
		{
			if (_values.Count == 1)
				return _values.First().ToString();

			var sb = new StringBuilder();
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
			return sb.ToString();
		}

		public override bool Equals(object obj)
		{
			var other = obj as SymbolicFeatureValue;
			return other != null && Equals(other);
		}

		public bool Equals(SymbolicFeatureValue other)
		{
			if (other == null)
				return false;
			other = Dereference(other);
			return _values.Equals(other._values);
		}

		public override int GetHashCode()
		{
			return _values.GetHashCode();
		}

		public override FeatureValue Clone()
		{
			return new SymbolicFeatureValue(this);
		}
	}
}
