using System.Collections.Generic;

namespace SIL.APRE
{
	public class ComplexFeature : Feature
	{
		private readonly IDBearerSet<Feature> _subfeatures;

		public ComplexFeature(string id, string description)
			: base(id, description)
		{
			_subfeatures = new IDBearerSet<Feature>();
		}

		public ComplexFeature(string id)
			: this(id, id)
		{
		}

		public override FeatureValueType ValueType
		{
			get { return FeatureValueType.Complex; }
		}

		public IEnumerable<Feature> Subfeatures
		{
			get { return _subfeatures; }
		}

		public void AddSubfeature(Feature feature)
		{
			feature.ParentFeature = this;
			_subfeatures.Add(feature);
		}

		public void RemoveSubfeature(Feature feature)
		{
			_subfeatures.Remove(feature);
		}

		public Feature GetSubfeature(string id)
		{
			Feature feature;
			if (_subfeatures.TryGetValue(id, out feature))
				return feature;
			return null;
		}
	}
}
