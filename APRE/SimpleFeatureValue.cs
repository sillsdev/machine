using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE
{
	public abstract class SimpleFeatureValue<T> : FeatureValue
	{
		private readonly HashSet<T> _values;

		protected SimpleFeatureValue(IEnumerable<T> values)
		{
			_values = new HashSet<T>(values);
		}

		protected SimpleFeatureValue(T value)
		{
			_values = new HashSet<T> { value };
		}

		protected SimpleFeatureValue(SimpleFeatureValue<T> fv)
		{
			_values = new HashSet<T>(fv._values);
		}

		public IEnumerable<T> Values
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

		public override bool Unify(FeatureValue other, out FeatureValue output, bool useDefaults)
		{
			var sfv = (SimpleFeatureValue<T>) other.Clone();
			if (sfv.UnifyWith(this, useDefaults))
			{
				output = sfv;
				return true;
			}
			output = null;
			return false;
		}

		public override bool Matches(FeatureValue other)
		{
			var sfv = (SimpleFeatureValue<T>) other;
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

			_values.IntersectWith(((SimpleFeatureValue<T>) other)._values);
			return true;
		}

		public override void Instantiate(FeatureValue other)
		{
			_values.IntersectWith(((SimpleFeatureValue<T>)other)._values);
		}

		public override void Uninstantiate(FeatureValue other)
		{
			_values.UnionWith(((SimpleFeatureValue<T>)other)._values);
		}

		public override int GetHashCode()
		{
			return _values.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as SimpleFeatureValue<T>);
		}

		public bool Equals(SimpleFeatureValue<T> other)
		{
			if (other == null)
				return false;
			return _values.Equals(other._values);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			bool firstValue = true;
			foreach (T value in _values)
			{
				if (!firstValue)
					sb.Append(", ");
				sb.Append(value.ToString());
				firstValue = false;
			}
			return sb.ToString();
		}
	}
}
