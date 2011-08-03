using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE
{
	public class StringFeatureValue : FeatureValue
	{
		private readonly HashSet<string> _values; 
		private bool _not;

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
				return _values;
			}
		}

		public override bool IsAmbiguous
		{
			get { return _not || _values.Count > 1; }
		}

		public override bool IsSatisfiable
		{
			get { return _not || _values.Any(); }
		}

		public override bool Matches(FeatureValue other)
		{
			var sfv = (StringFeatureValue) other;
			if (_not == sfv._not)
				return _values.Any(value => sfv._values.Contains(value));

			return !_values.Equals(sfv._values);
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
			var sfv = (StringFeatureValue) other;
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
		}

		public override void UnionWith(FeatureValue other)
		{
			var sfv = (StringFeatureValue) other;
			if (!_not && !sfv._not)
			{
				_values.UnionWith(sfv._values);
			}
			else if (!_not && sfv._not)
			{
				_values.Clear();
				_values.UnionWith(sfv._values);
				_not = true;
			}
			else if (_not && !sfv._not)
			{
				// do nothing
			}
			else
			{
				_values.IntersectWith(sfv._values);
			}
		}

		public override void UninstantiateAll()
		{
			_not = true;
			_values.Clear();
		}

		public override void Negate()
		{
			_not = !_not;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as StringFeatureValue);
		}

		public bool Equals(StringFeatureValue other)
		{
			if (other == null)
				return false;
			return Values.Equals(other.Values) && _not == other._not;
		}

		public override int GetHashCode()
		{
			return _values.GetHashCode() ^ _not.GetHashCode();
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			bool firstValue = true;
			if (_not)
				sb.Append("!");
			sb.Append("{");
			foreach (string value in _values)
			{
				if (!firstValue)
					sb.Append(", ");
				sb.Append(value);
				firstValue = false;
			}
			sb.Append("}");
			return sb.ToString();
		}

		public override FeatureValue Clone()
		{
			return new StringFeatureValue(this);
		}
	}
}
