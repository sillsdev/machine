using System;
using System.Collections.Generic;
using System.Linq;
using SIL.APRE.FeatureModel.Fluent;

namespace SIL.APRE.FeatureModel
{
    /// <summary>
    /// This class represents a feature system. It encapsulates all of the valid features and symbols.
    /// </summary>
    public class FeatureSystem
    {
		public static IFeatureSystemSyntax New()
		{
			return new FeatureSystemBuilder();
		}

    	private readonly IDBearerSet<Feature> _features;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureSystem"/> class.
        /// </summary>
        public FeatureSystem()
        {
            _features = new IDBearerSet<Feature>();
        }

        /// <summary>
        /// Gets the features.
        /// </summary>
        /// <value>The features.</value>
        public IEnumerable<Feature> Features
        {
            get
            {
                return _features;
            }
        }

        public int NumFeatures
        {
            get
            {
                return _features.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this feature system has features.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance has features, otherwise <c>false</c>.
        /// </value>
        public bool HasFeatures
        {
            get
            {
                return _features.Count > 0;
            }
        }

    	/// <summary>
        /// Gets the feature associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The feature.</returns>
        public Feature GetFeature(string id)
        {
            Feature feature;
            if (_features.TryGetValue(id, out feature))
                return feature;

			foreach (ComplexFeature child in _features.OfType<ComplexFeature>())
			{
				if (FindFeature(id, child, out feature))
					return feature;
			}

			throw new ArgumentException("The specified feature could not be found.", "id");
        }

		private bool FindFeature(string id, ComplexFeature feature, out Feature result)
		{
			if (feature.TryGetSubfeature(id, out result))
				return true;

			foreach (ComplexFeature child in feature.Subfeatures.OfType<ComplexFeature>())
			{
				if (FindFeature(id, child, out result))
					return true;
			}

			return false;
		}

        /// <summary>
        /// Gets the feature value associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The feature value.</returns>
        public FeatureSymbol GetSymbol(string id)
        {
        	FeatureSymbol symbol;
			if (FindSymbol(id, _features, out symbol))
				return symbol;

			throw new ArgumentException("The specified symbol could not be found.", "id");
        }

		private bool FindSymbol(string id, IEnumerable<Feature> features, out FeatureSymbol result)
		{
			foreach (Feature feature in features)
			{
				var sf = feature as SymbolicFeature;
				if (sf != null)
				{
					if (sf.TryGetPossibleSymbol(id, out result))
						return true;
				}
				else
				{
					var cf = feature as ComplexFeature;
					if (cf != null)
					{
						if (FindSymbol(id, cf.Subfeatures, out result))
							return true;
					}
				}
			}

			result = null;
			return false;
		}

        /// <summary>
        /// Adds the feature.
        /// </summary>
        /// <param name="feature">The feature.</param>
        public virtual void AddFeature(Feature feature)
        {
        	_features.Add(feature);
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public virtual void Reset()
        {
            _features.Clear();
        }
    }
}
