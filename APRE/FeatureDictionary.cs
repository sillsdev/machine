using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE
{
	/// <summary>
	/// This class represents a set of feature values. It differs from <see cref="SymbolicFeatureValueSet"/> in that
	/// it does not represent the values as bits in unsigned long integers. It is primarily used for
	/// morphosyntatic features.
	/// </summary>
	public class FeatureDictionary : FeatureStructure
	{
		private readonly SortedDictionary<Feature, FeatureValue> _values;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureDictionary"/> class.
		/// </summary>
		public FeatureDictionary(FeatureSystem featSys)
			: base(featSys)
		{
			_values = new SortedDictionary<Feature, FeatureValue>();
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="fv">The fs.</param>
		public FeatureDictionary(FeatureDictionary fv)
			: this(fv.FeatureSystem)
		{
			foreach (KeyValuePair<Feature, FeatureValue> kvp in fv._values)
				_values[kvp.Key] = kvp.Value.Clone();
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public override IEnumerable<Feature> Features
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
		public override int NumFeatures
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
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Adds the specified feature-value pair.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <param name="value">The value.</param>
		public override void Add(Feature feature, FeatureValue value)
		{
			_values[feature] = value;
		}

		public override void Add(IEnumerable<Feature> path, FeatureValue value)
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

		public override void Clear()
		{
			_values.Clear();
		}

		/// <summary>
		/// Gets the values for the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>All values.</returns>
		public override FeatureValue GetValue(Feature feature)
		{
			FeatureValue value;
			if (_values.TryGetValue(feature, out value))
				return value;
			return null;
		}

		public override FeatureValue GetValue(IEnumerable<Feature> path)
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

		/// <summary>
		/// Determines whether the specified feature values set matches this set of feature
		/// values. For each feature in this set, there must be a value which belongs to
		/// the list of values in the specified set.
		/// </summary>
		/// <param name="fs">The feature values.</param>
		/// <returns>
		/// 	<c>true</c> if the sets match, otherwise <c>false</c>.
		/// </returns>
		public override bool Matches(FeatureStructure fs)
		{
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
		/// <param name="fs">The feature values.</param>
		/// <returns>
		/// 	<c>true</c> the sets are compatible, otherwise <c>false</c>.
		/// </returns>
		public override bool IsUnifiable(FeatureStructure fs)
		{
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

		public override bool UnifyWith(FeatureStructure fs, bool useDefaults)
		{
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

		public override void Instantiate(FeatureStructure other)
		{
			throw new NotImplementedException();
		}

		public override void Uninstantiate(FeatureStructure other)
		{
			throw new NotImplementedException();
		}

		public override void UninstantiateAll()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the difference between this subset and the specified superset. If this set is
		/// not a subset of the specified superset, it will return <c>false</c>.
		/// </summary>
		/// <param name="superset">The superset feature values.</param>
		/// <param name="remainder">The remainder.</param>
		/// <returns><c>true</c> if this is a subset, otherwise <c>false</c>.</returns>
		public bool GetSupersetRemainder(FeatureDictionary superset, out FeatureDictionary remainder)
		{
			var result = (FeatureDictionary)superset.Clone();
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
			return new FeatureDictionary(this);
		}

		public override int GetHashCode()
		{
			return _values.Aggregate(0, (current, kvp) => current ^ (kvp.Key.GetHashCode() ^ (kvp.Value != null ? kvp.Value.GetHashCode() : 0)));
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as FeatureDictionary);
		}

		public bool Equals(FeatureDictionary other)
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
			foreach (KeyValuePair<Feature, FeatureValue> kvp in _values)
			{
				if (!firstFeature)
					sb.Append(", ");
				sb.Append(kvp.Key.Description);
				if (kvp.Value != null)
				{
					sb.Append("->(");
					sb.Append(kvp.Value.ToString());
					sb.Append(")");
				}
				firstFeature = false;
			}

			return sb.ToString();
		}
	}
}
