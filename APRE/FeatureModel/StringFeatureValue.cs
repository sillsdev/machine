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
			: this(Enumerable.Empty<string>())
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

		public IEnumerable<string> Values
		{
			get { return _values; }
		}

		public bool Not
		{
			get { return _not; }
		}

		public bool Contains(string str)
		{
			return _not ? !_values.Contains(str) : _values.Contains(str);
		}

		public bool Overlaps(IEnumerable<string> strings)
		{
			return _not ? !_values.Overlaps(strings) : _values.Overlaps(strings);
		}

		protected override bool Overlaps(FeatureValue other, bool negate)
		{
			var otherSfv = other as StringFeatureValue;
			if (otherSfv == null)
				return false;
			
			bool not = negate ? !otherSfv._not : otherSfv._not;

			if (!_not && !not)
			{
				return _values.Overlaps(otherSfv._values);
			}
			if (!_not && not)
			{
				return _values.IsSupersetOf(otherSfv._values);
			}
			if (_not && !not)
			{
				return _values.IsProperSubsetOf(otherSfv._values);
			}

			return true;
		}

		protected override void IntersectWith(FeatureValue other, bool negate)
		{
			var otherSfv = (StringFeatureValue) other;
			bool not = negate ? !otherSfv._not : otherSfv._not;

			if (!_not && !not)
			{
				_not = false;
				_values.IntersectWith(otherSfv._values);
			}
			else if (!_not && not)
			{
				_not = false;
				_values.ExceptWith(otherSfv._values);
			}
			else if (_not && !not)
			{
				_not = false;
				string[] newValues = otherSfv._values.Except(_values).ToArray();
				_values.Clear();
				_values.UnionWith(newValues);
			}
			else
			{
				_not = true;
				_values.UnionWith(otherSfv._values);
			}
		}

		protected override void UnionWith(FeatureValue other, bool negate)
		{
			var otherSfv = (StringFeatureValue) other;
			bool not = negate ? !otherSfv._not : otherSfv._not;

			if (!_not && !not)
			{
				_not = false;
				_values.UnionWith(otherSfv._values);
			}
			else if (!_not && not)
			{
				_not = true;
				_values.ExceptWith(otherSfv._values);
			}
			else if (_not && !not)
			{
				_not = true;
				string[] newValues = otherSfv._values.Except(_values).ToArray();
				_values.Clear();
				_values.UnionWith(newValues);
			}
			else
			{
				_not = true;
				_values.IntersectWith(otherSfv.Values);
			}
		}

		public override bool Negation(out FeatureValue output)
		{
			output = new StringFeatureValue(_values, !_not);
			return true;
		}

		public override bool Equals(object obj)
		{
			var other = obj as StringFeatureValue;
			return other != null && Equals(other);
		}

		public bool Equals(StringFeatureValue other)
		{
			if (other == null)
				return false;

			other = Dereference(other);
			return _values.SetEquals(other._values) && _not == other._not;
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
				sb.Append('!');
			if (_values.Count == 1)
			{
				sb.Append('"');
				sb.Append(_values.First());
				sb.Append('"');
			}
			else
			{
				sb.Append('{');
				foreach (string value in _values)
				{
					if (!firstValue)
						sb.Append(", ");
					sb.Append('"');
					sb.Append(value);
					sb.Append('"');
					firstValue = false;
				}
				sb.Append('}');
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
