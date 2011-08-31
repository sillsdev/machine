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
			if (!values.Any())
				throw new ArgumentException("values cannot be empty", "values");
			_feature = values.First().Feature;
			_values = new IDBearerSet<FeatureSymbol>(values);
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

		public override FeatureValueType Type
		{
			get { return FeatureValueType.Symbol; }
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

		protected override bool IsValuesUnifiable(FeatureValue other, bool negate)
		{
			if (other.Type != FeatureValueType.Symbol)
				return false;

			var otherSfv = (SymbolicFeatureValue) other;
			IEnumerable<FeatureSymbol> values = negate ? otherSfv._feature.PossibleSymbols.Except(otherSfv._values) : otherSfv._values;
			return _values.Overlaps(values);
		}

		protected override void UnifyValues(FeatureValue other, bool negate)
		{
			var otherSfv = (SymbolicFeatureValue) other;
			IEnumerable<FeatureSymbol> values = negate ? otherSfv._feature.PossibleSymbols.Except(otherSfv._values) : otherSfv._values;
			_values.IntersectWith(values);
		}

		internal override bool Negation(out FeatureValue output)
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
