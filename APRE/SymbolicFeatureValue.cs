using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE
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
			get
			{
				if (Forward != null)
					return ((SymbolicFeatureValue) Forward).Values;

				return _values;
			}
		}

		internal override bool IsUnifiable(FeatureValue other, bool useDefaults, IDictionary<string, FeatureValue> varBindings)
		{
			if (Forward != null)
				return Forward.IsUnifiable(other, useDefaults, varBindings);

			SymbolicFeatureValue sfv;
			if (!GetValue(other, out sfv))
				return false;
			return _values.Overlaps(sfv._values);
		}

		internal override bool DestructiveUnify(FeatureValue other, bool useDefaults, bool preserveInput,
			IDictionary<FeatureValue, FeatureValue> copies, IDictionary<string, FeatureValue> varBindings)
		{
			if (Forward != null)
				return Forward.DestructiveUnify(other, useDefaults, preserveInput, copies, varBindings);

			SymbolicFeatureValue sfv;
			if (!GetValue(other, out sfv))
				return false;

			if (!IsUnifiable(sfv, useDefaults, varBindings))
				return false;

			if (preserveInput)
			{
				if (copies != null)
					copies[sfv] = this;
			}
			else
			{
				sfv.Forward = this;
			}

			_values.IntersectWith(sfv._values);
			return true;
		}

		internal override bool Negation(out FeatureValue output)
		{
			if (Forward != null)
				return Forward.Negation(out output);

			output = new SymbolicFeatureValue(_feature.PossibleSymbols.Except(_values));
			return true;
		}

		public override string ToString()
		{
			if (Forward != null)
				return Forward.ToString();

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
			if (Forward != null)
				return Forward.Equals(obj);

			if (obj == null)
				return false;
			return Equals(obj as SymbolicFeatureValue);
		}

		public bool Equals(SymbolicFeatureValue other)
		{
			if (Forward != null)
				return ((SymbolicFeatureValue) Forward).Equals(other);

			if (other == null)
				return false;
			other = GetValue(other);
			return _values.Equals(other._values);
		}

		public override int GetHashCode()
		{
			return _values.GetHashCode();
		}

		public override FeatureValue Clone()
		{
			if (Forward != null)
				return Forward.Clone();

			return new SymbolicFeatureValue(this);
		}
	}
}
