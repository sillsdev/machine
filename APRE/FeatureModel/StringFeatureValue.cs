using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE.FeatureModel
{
	public class StringFeatureValue : SimpleFeatureValue
	{
		private readonly HashSet<string> _values; 
		private bool _not;

		public StringFeatureValue()
			: this((IEnumerable<string>) Enumerable.Empty<string>())
		{
		}

		public StringFeatureValue(IEnumerable<string> values)
			: this(values, false)
		{
		}

		public StringFeatureValue(IEnumerable<string> values, bool not)
		{
			_values = new HashSet<string>(values);
			_not = not;
		}

		public StringFeatureValue(string value)
			: this(value, false)
		{
		}

		public StringFeatureValue(string value, bool not)
		{
			_values = new HashSet<string> {value};
			_not = not;
		}

		public StringFeatureValue(StringFeatureValue sfv)
		{
			_values = new HashSet<string>(sfv._values);
			_not = sfv._not;
		}

		public override FeatureValueType Type
		{
			get { return FeatureValueType.String; }
		}

		public IEnumerable<string> Values
		{
			get
			{
				if (Forward != null)
					return ((StringFeatureValue) Forward).Values;

				return _values;
			}
		}

		public bool Not
		{
			get
			{
				if (Forward != null)
					return ((StringFeatureValue) Forward).Not;

				return _not;
			}
		}

		internal override bool IsUnifiable(FeatureValue other, bool useDefaults, IDictionary<string, FeatureValue> varBindings)
		{
			if (Forward != null)
				return Forward.IsUnifiable(other, useDefaults, varBindings);

			StringFeatureValue sfv;
			if (!GetValue(other, out sfv))
				return false;

			if (!_not && !sfv._not)
			{
				return _values.Overlaps(sfv._values);
			}
			if (!_not && sfv._not)
			{
				return !_values.SetEquals(sfv._values);
			}
			if (_not && !sfv._not)
			{
				return !_values.SetEquals(sfv._values);
			}

			return true;
		}

		internal override bool DestructiveUnify(FeatureValue other, bool useDefaults, bool preserveInput,
			IDictionary<FeatureValue, FeatureValue> copies, IDictionary<string, FeatureValue> varBindings)
		{
			if (Forward != null)
				return Forward.DestructiveUnify(other, useDefaults, preserveInput, copies, varBindings);

			StringFeatureValue sfv;
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

			if (!_not && !sfv._not)
			{
				_values.IntersectWith(sfv._values);
			}
			else if (!_not && sfv._not)
			{
				_values.ExceptWith(sfv._values);
			}
			else if (_not && !sfv._not)
			{
				string[] newValues = sfv._values.Except(_values).ToArray();
				_values.Clear();
				_values.UnionWith(newValues);
				_not = false;
			}
			else
			{
				_values.UnionWith(sfv._values);
			}
			return true;
		}

		internal override bool Negation(out FeatureValue output)
		{
			if (Forward != null)
				return Forward.Negation(out output);

			output = new StringFeatureValue(_values, !_not);
			return true;
		}

		public override bool Equals(object obj)
		{
			if (Forward != null)
				return Forward.Equals(obj);

			if (obj == null)
				return false;
			return Equals(obj as StringFeatureValue);
		}

		public bool Equals(StringFeatureValue other)
		{
			if (Forward != null)
				return ((StringFeatureValue) Forward).Equals(other);

			if (other == null)
				return false;

			other = GetValue(other);
			return _values.SetEquals(other._values) && _not == other._not;
		}

		public override int GetHashCode()
		{
			if (Forward != null)
				return Forward.GetHashCode();

			return _values.GetHashCode() ^ _not.GetHashCode();
		}

		public override string ToString()
		{
			if (Forward != null)
				return Forward.ToString();

			var sb = new StringBuilder();
			bool firstValue = true;
			if (_not)
				sb.Append("!");
			if (_values.Count == 1)
			{
				sb.Append(_values.First());
			}
			else
			{
				sb.Append("{");
				foreach (string value in _values)
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

		public override FeatureValue Clone()
		{
			if (Forward != null)
				return Forward.Clone();

			return new StringFeatureValue(this);
		}
	}
}
