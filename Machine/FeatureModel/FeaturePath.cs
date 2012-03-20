using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.FeatureModel
{
	public class FeaturePath : IReadOnlyList<Feature>, IEquatable<FeaturePath>
	{
		private readonly List<Feature> _features;

		public FeaturePath(params Feature[] features)
		{
			_features = features.ToList();
		}

		public FeaturePath(IEnumerable<Feature> features)
		{
			_features = features.ToList();
		}

		public IEnumerator<Feature> GetEnumerator()
		{
			return _features.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int Count
		{
			get { return _features.Count; }
		}

		public Feature this[int index]
		{
			get { return _features[index]; }
		}

		public bool Equals(FeaturePath other)
		{
			return other != null && this.SequenceEqual(other);
		}

		public override bool Equals(object obj)
		{
			var other = obj as FeaturePath;
			return other != null && Equals(other);
		}

		public override int GetHashCode()
		{
			return _features.Aggregate(23, (code, feature) => code * 31 + feature.GetHashCode());
		}
	}
}
