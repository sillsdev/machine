using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE
{
	public class FeatureStructure : FeatureValue
	{
		private readonly FeatureSystem _featSys;
		private readonly SortedDictionary<Feature, FeatureValue> _values;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureStructure"/> class.
		/// </summary>
		public FeatureStructure(FeatureSystem featSys)
		{
			_featSys = featSys;
			_values = new SortedDictionary<Feature, FeatureValue>();
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="fv">The fs.</param>
		public FeatureStructure(FeatureStructure fv)
			: this(fv.FeatureSystem)
		{
			foreach (KeyValuePair<Feature, FeatureValue> kvp in fv._values)
				_values[kvp.Key] = kvp.Value.Clone();
		}


		public FeatureSystem FeatureSystem
		{
			get { return _featSys; }
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public IEnumerable<Feature> Features
		{
			get
			{
				return _values.Keys;
			}
		}

		/// <summary>
		/// Gets the number of features.
		/// </summary>
		/// <value>The number of features.</value>
		public int NumFeatures
		{
			get
			{
				return _values.Count;
			}
		}

		public override FeatureValueType Type
		{
			get
			{
				return FeatureValueType.Complex;
			}
		}

		public override bool IsAmbiguous
		{
			get { return _values.Values.Any(value => value.IsAmbiguous); }
		}

		public override bool IsSatisfiable
		{
			get { return _values.Values.All(value => value.IsSatisfiable); }
		}

		/// <summary>
		/// Adds the specified feature-value pair.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <param name="value">The value.</param>
		public void Add(Feature feature, FeatureValue value)
		{
			_values[feature] = value;
		}

		public void Add(IEnumerable<Feature> path, FeatureValue value)
		{
			Feature f = path.First();
			IEnumerable<Feature> remaining = path.Skip(1);
			if (remaining.Any())
			{
				FeatureValue curValue;
				if (_values.TryGetValue(f, out curValue))
				{
					var fs = curValue as FeatureStructure;
					if (fs != null)
						fs.Add(remaining, value);
					else
						throw new ArgumentException("The feature path is invalid.", "path");
				}
				else
				{
					throw new ArgumentException("The feature path is invalid.", "path");
				}
			}
			else
			{
				Add(f, value);
			}
		}

		public void Clear()
		{
			_values.Clear();
		}

		/// <summary>
		/// Gets the values for the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>All values.</returns>
		public FeatureValue GetValue(Feature feature)
		{
			FeatureValue value;
			if (_values.TryGetValue(feature, out value))
				return value;
			return null;
		}

		public FeatureValue GetValue(IEnumerable<Feature> path)
		{
			Feature f = path.First();
			IEnumerable<Feature> remaining = path.Skip(1);
			if (remaining.Any())
			{
				FeatureValue curValue;
				if (_values.TryGetValue(f, out curValue))
				{
					var fs = curValue as FeatureStructure;
					if (fs != null)
						return fs.GetValue(remaining);
				}
				return null;
			}

			return GetValue(f);
		}

		public FeatureValue GetValue(string id)
		{
			Feature feature = _featSys.GetFeature(id);
			if (feature != null)
				return GetValue(feature);
			return null;
		}

		/// <summary>
		/// Determines whether the specified feature values set matches this set of feature
		/// values. For each feature in this set, there must be a value which belongs to
		/// the list of values in the specified set.
		/// </summary>
		/// <param name="other">The feature value.</param>
		/// <returns>
		/// 	<c>true</c> if the sets match, otherwise <c>false</c>.
		/// </returns>
		public override bool Matches(FeatureValue other)
		{
			var fs = (FeatureStructure) other;
			foreach (KeyValuePair<Feature, FeatureValue> kvp in _values)
			{
				FeatureValue value = fs.GetValue(kvp.Key);
				if (value == null)
					return false;

				if (kvp.Value != null && !kvp.Value.Matches(value))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Determines whether the specified set of feature values is compatible with this
		/// set of feature values. It is much like <c>Matches</c> except that if a the
		/// specified set does not contain a feature in this set, it is still a match.
		/// It basically checks to make sure that there is no contradictory features.
		/// </summary>
		/// <param name="other">The feature value.</param>
		/// <returns>
		/// 	<c>true</c> the sets are compatible, otherwise <c>false</c>.
		/// </returns>
		public override bool IsUnifiable(FeatureValue other)
		{
			var fs = (FeatureStructure) other;
			foreach (KeyValuePair<Feature, FeatureValue> kvp in _values)
			{
				FeatureValue value = fs.GetValue(kvp.Key);
				if (value == null)
					continue;

				if (kvp.Value != null && !kvp.Value.IsUnifiable(value))
					return false;
			}
			return true;
		}

		public override bool UnifyWith(FeatureValue other, bool useDefaults)
		{
			var fs = (FeatureStructure) other;
			var unification = new Dictionary<Feature, FeatureValue>(_values);
			foreach (Feature feature in fs.Features)
			{
				FeatureValue value = fs.GetValue(feature);
				FeatureValue curValue;
				if (_values.TryGetValue(feature, out curValue))
				{
					FeatureValue newValue = curValue.Clone();
					if (!newValue.UnifyWith(value, useDefaults))
						return false;
					unification[feature] = newValue;
				}
				else if (useDefaults && feature.DefaultValue != null)
				{
					FeatureValue defValue = feature.DefaultValue.Clone();
					if (!defValue.UnifyWith(value, true))
						return false;
					unification[feature] = defValue;
				}
				else
				{
					unification[feature] = value.Clone();
				}
			}

			foreach (KeyValuePair<Feature, FeatureValue> kvp in unification)
				_values[kvp.Key] = kvp.Value;
			return true;
		}

		public override void IntersectWith(FeatureValue other)
		{
			var fs = (FeatureStructure) other;
			foreach (Feature feature in fs.Features)
			{
				FeatureValue value = fs.GetValue(feature);
				FeatureValue curValue;
				if (_values.TryGetValue(feature, out curValue))
					curValue.IntersectWith(value);
			}

			foreach (Feature feature in _values.Keys.Except(fs.Features).ToArray())
				_values.Remove(feature);
		}

		public override void UnionWith(FeatureValue other)
		{
			var fs = (FeatureStructure)other;
			foreach (Feature feature in fs.Features)
			{
				FeatureValue value = fs.GetValue(feature);
				FeatureValue curValue;
				if (_values.TryGetValue(feature, out curValue))
					curValue.UnionWith(value);
				else
					_values[feature] = value.Clone();
			}
		}

		public override void UninstantiateAll()
		{
			foreach (FeatureValue value in _values.Values)
				value.UninstantiateAll();
		}

		public override void Negate()
		{
			foreach (FeatureValue value in _values.Values)
				value.Negate();
		}

		public bool Unify(FeatureStructure other, out FeatureStructure output, bool useDefaults)
		{
			var fs = (FeatureStructure) other.Clone();
			if (fs.UnifyWith(this, useDefaults))
			{
				output = fs;
				return true;
			}
			output = null;
			return false;
		}

		public FeatureStructure Negation()
		{
			var fs = (FeatureStructure) Clone();
			fs.Negate();
			return fs;
		}

		public FeatureStructure Intersect(FeatureStructure other)
		{
			var fs = (FeatureStructure) other.Clone();
			fs.IntersectWith(this);
			return fs;
		}

		public FeatureStructure Union(FeatureStructure other)
		{
			var fs = (FeatureStructure) other.Clone();
			fs.UnionWith(this);
			return fs;
		}

		/// <summary>
		/// Gets the difference between this subset and the specified superset. If this set is
		/// not a subset of the specified superset, it will return <c>false</c>.
		/// </summary>
		/// <param name="superset">The superset feature values.</param>
		/// <param name="remainder">The remainder.</param>
		/// <returns><c>true</c> if this is a subset, otherwise <c>false</c>.</returns>
		public bool GetSupersetRemainder(FeatureStructure superset, out FeatureStructure remainder)
		{
			var result = (FeatureStructure) superset.Clone();
			foreach (KeyValuePair<Feature, FeatureValue> kvp in _values)
			{
				FeatureValue value;
				if (kvp.Value != null && (!result._values.TryGetValue(kvp.Key, out value) || !value.Equals(kvp.Value)))
				{
					remainder = null;
					return false;
				}

				result._values.Remove(kvp.Key);
			}

			remainder = result;
			return true;
		}

		public override FeatureValue Clone()
		{
			return new FeatureStructure(this);
		}

		public override int GetHashCode()
		{
			return _values.Aggregate(0, (current, kvp) => current ^ (kvp.Key.GetHashCode() ^ (kvp.Value != null ? kvp.Value.GetHashCode() : 0)));
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as FeatureStructure);
		}

		public bool Equals(FeatureStructure other)
		{
			if (other == null)
				return false;

			if (_values.Count != other._values.Count)
				return false;

			foreach (KeyValuePair<Feature, FeatureValue> kvp in _values)
			{
				FeatureValue value;
				if (!other._values.TryGetValue(kvp.Key, out value))
					return false;

				if (kvp.Value != null && !kvp.Value.Equals(value))
					return false;
			}

			return true;
		}

		public override string ToString()
		{
			bool firstFeature = true;
			var sb = new StringBuilder();
			sb.Append("[");
			foreach (KeyValuePair<Feature, FeatureValue> kvp in _values)
			{
				if (!firstFeature)
					sb.Append(", ");
				sb.Append(kvp.Key.Description);
				if (kvp.Value != null)
				{
					sb.Append("->");
					sb.Append(kvp.Value.ToString());
				}
				firstFeature = false;
			}
			sb.Append("]");
			return sb.ToString();
		}
	}
}
