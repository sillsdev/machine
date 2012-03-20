using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel.Fluent;

namespace SIL.Machine.FeatureModel
{
    /// <summary>
    /// This class represents a feature system. It encapsulates all of the valid features and symbols.
    /// </summary>
    public class FeatureSystem : ICollection<Feature>
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

		public Feature GetFeature(string id)
		{
			Feature feature;
			if (TryGetFeature(id, out feature))
				return feature;

			throw new ArgumentException("The specified feature could not be found.", "id");
		}

		public T GetFeature<T>(string id) where T : Feature
		{
			Feature feature;
			if (TryGetFeature(id, out feature))
				return (T) feature;

			throw new ArgumentException("The specified feature could not be found.", "id");
		}

		public bool TryGetFeature(string id, out Feature feature)
		{
			return _features.TryGetValue(id, out feature);
		}

		public bool TryGetFeature<T>(string id, out T feature) where T : Feature
		{
			Feature f;
			if (TryGetFeature(id, out f))
			{
				feature = (T) f;
				return true;
			}

			feature = null;
			return false;
		}

		public Feature this[string id]
		{
			get
			{
				Feature feature;
				if (TryGetFeature(id, out feature))
					return feature;

				throw new ArgumentException("The specified feature could not be found.", "id");
			}
		}

        /// <summary>
        /// Gets the feature value associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>The feature value.</returns>
        public FeatureSymbol GetSymbol(string id)
        {
        	FeatureSymbol symbol;
			if (TryGetSymbol(id, out symbol))
				return symbol;

			throw new ArgumentException("The specified symbol could not be found.", "id");
        }

    	/// <summary>
    	/// Gets the feature value associated with the specified ID.
    	/// </summary>
    	/// <param name="id">The ID.</param>
    	/// <param name="symbol"> </param>
    	/// <returns>The feature value.</returns>
    	public bool TryGetSymbol(string id, out FeatureSymbol symbol)
		{
			foreach (SymbolicFeature sf in _features.OfType<SymbolicFeature>())
			{
				if (sf.PossibleSymbols.TryGetValue(id, out symbol))
					return true;
			}

			symbol = null;
			return false;
		}

    	IEnumerator<Feature> IEnumerable<Feature>.GetEnumerator()
    	{
    		return ((IEnumerable<Feature>) _features).GetEnumerator();
    	}

    	IEnumerator IEnumerable.GetEnumerator()
    	{
    		return ((IEnumerable<Feature>) this).GetEnumerator();
    	}

    	public void Add(Feature feature)
    	{
			if (IsReadOnly)
				throw new InvalidOperationException("The feature system is read-only.");
			_features.Add(feature);
    	}

    	public void Clear()
    	{
			if (IsReadOnly)
				throw new InvalidOperationException("The feature system is read-only.");
    		_features.Clear();
    	}

		bool ICollection<Feature>.Contains(Feature feature)
		{
			return _features.Contains(feature);
		}

    	void ICollection<Feature>.CopyTo(Feature[] array, int arrayIndex)
    	{
    		_features.CopyTo(array, arrayIndex);
    	}

		public bool Remove(Feature feature)
		{
			if (IsReadOnly)
				throw new InvalidOperationException("The feature system is read-only.");
			return _features.Remove(feature);
		}

		public bool Remove(string featureID)
		{
			if (IsReadOnly)
				throw new InvalidOperationException("The feature system is read-only.");
			return _features.Remove(featureID);
		}

    	public int Count
    	{
			get { return _features.Count; }
    	}

		public bool IsReadOnly { get; protected set; }
    }
}
