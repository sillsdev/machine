using System.Collections.Generic;

namespace SIL.APRE
{
	public abstract class FeatureStructure : FeatureValue
	{
		private readonly FeatureSystem _featSys;

		protected FeatureStructure(FeatureSystem featSys)
		{
			_featSys = featSys;
		}

		public FeatureSystem FeatureSystem
		{
			get { return _featSys; }
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public abstract IEnumerable<Feature> Features
		{
			get;
		}

		/// <summary>
		/// Gets the number of features.
		/// </summary>
		/// <value>The number of features.</value>
		public abstract int NumFeatures
		{
			get;
		}

		public abstract void Add(Feature feature, FeatureValue value);

		public abstract void Add(IEnumerable<Feature> path, FeatureValue value);

		public abstract void Clear();

		/// <summary>
		/// Gets the values for the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>All values.</returns>
		public abstract FeatureValue GetValue(Feature feature);

		public abstract FeatureValue GetValue(IEnumerable<Feature> path);

		public FeatureValue GetValue(string id)
		{
			Feature feature = _featSys.GetFeature(id);
			if (feature != null)
				return GetValue(feature);
			return null;
		}
		
		public bool Unify(FeatureStructure other, out FeatureStructure output, bool useDefaults)
		{
			output = (FeatureStructure) other.Clone();
			if (output.UnifyWith(this, useDefaults))
				return true;
			output = null;
			return false;
		}

		public abstract bool Matches(FeatureStructure other);
		public abstract bool IsUnifiable(FeatureStructure other);
		public abstract bool UnifyWith(FeatureStructure other, bool useDefaults);

		public abstract void Instantiate(FeatureStructure other);
		public abstract void Uninstantiate(FeatureStructure other);

		public override bool Unify(FeatureValue other, out FeatureValue output, bool useDefaults)
		{
			var fs = other as FeatureStructure;
			if (fs != null)
			{
				FeatureStructure outputFs;
				if (Unify(other as FeatureStructure, out outputFs, useDefaults))
				{
					output = outputFs;
					return true;
				}
			}

			output = null;
			return false;
		}

		public override bool Matches(FeatureValue other)
		{
			var fs = other as FeatureStructure;
			if (fs == null)
				return false;
			return Matches(fs);
		}

		public override bool IsUnifiable(FeatureValue other)
		{
			var fs = other as FeatureStructure;
			if (fs == null)
				return false;
			return IsUnifiable(fs);
		}

		public override bool UnifyWith(FeatureValue other, bool useDefaults)
		{
			var fs = other as FeatureStructure;
			if (fs == null)
				return false;
			return UnifyWith(fs, useDefaults);
		}

		public override void Instantiate(FeatureValue other)
		{
			var fs = other as FeatureStructure;
			if (fs != null)
				Instantiate(fs);
		}

		public override void Uninstantiate(FeatureValue other)
		{
			var fs = other as FeatureStructure;
			if (fs != null)
				Uninstantiate(fs);
		}
	}
}
