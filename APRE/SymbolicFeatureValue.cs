using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE
{
	public class SymbolicFeatureValue : FeatureValue
	{
		private readonly SymbolicFeature _feature;
		private readonly HashSet<FeatureSymbol> _values;

		public SymbolicFeatureValue(IEnumerable<FeatureSymbol> values)
		{
			if (!values.Any())
				throw new ArgumentException("values cannot be empty", "values");
			_feature = values.First().Feature;
			_values = new HashSet<FeatureSymbol>(values);
		}

		public SymbolicFeatureValue(FeatureSymbol value)
		{
			_feature = value.Feature;
			_values = new HashSet<FeatureSymbol> {value};
		}

		public SymbolicFeatureValue(SymbolicFeatureValue sfv)
		{
			_feature = sfv._feature;
			_values = new HashSet<FeatureSymbol>(sfv._values);
		}

		public override FeatureValueType Type
		{
			get { return FeatureValueType.Symbol; }
		}

		public IEnumerable<FeatureSymbol> Values
		{
			get
			{
				return _values;
			}
		}

		public override bool IsAmbiguous
		{
			get { return _values.Count > 1; }
		}

		public override bool IsSatisfiable
		{
			get { return _values.Any(); }
		}

		public override bool Matches(FeatureValue other)
		{
			var sfv = (SymbolicFeatureValue) other;
			return _values.Any(value => sfv._values.Contains(value));
		}

		public override bool IsUnifiable(FeatureValue other)
		{
			return Matches(other);
		}

		public override bool UnifyWith(FeatureValue other, bool useDefaults)
		{
			if (!IsUnifiable(other))
				return false;

			IntersectWith(other);
			return true;
		}

		public override void IntersectWith(FeatureValue other)
		{
			_values.IntersectWith(((SymbolicFeatureValue) other)._values);
		}

		public override void UnionWith(FeatureValue other)
		{
			_values.UnionWith(((SymbolicFeatureValue) other)._values);
		}

		public override void UninstantiateAll()
		{
			_values.UnionWith(_feature.PossibleSymbols);
		}

		public override void Negate()
		{
			FeatureSymbol[] symbols = _feature.PossibleSymbols.Except(_values).ToArray();
			_values.Clear();
			_values.UnionWith(symbols);
		}

		public override string ToString()
		{
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
			if (obj == null)
				return false;
			return Equals(obj as SymbolicFeatureValue);
		}

		public bool Equals(SymbolicFeatureValue other)
		{
			if (other == null)
				return false;
			return Values.Equals(other.Values);
		}

		public override int GetHashCode()
		{
			return Values.GetHashCode();
		}

		public override FeatureValue Clone()
		{
			return new SymbolicFeatureValue(this);
		}
	}
}
