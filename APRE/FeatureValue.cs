using System;

namespace SIL.APRE
{
	public enum FeatureValueType { Symbol, Complex, String };

	public abstract class FeatureValue : ICloneable
	{
		public abstract FeatureValueType Type { get; }

		public abstract bool IsAmbiguous { get; }
		public abstract bool IsSatisfiable { get; }

		public abstract bool Matches(FeatureValue other);
		public abstract bool IsUnifiable(FeatureValue other);
		public abstract bool UnifyWith(FeatureValue other, bool useDefaults);
		public abstract void IntersectWith(FeatureValue other);
		public abstract void UnionWith(FeatureValue other);
		public abstract void UninstantiateAll();
		public abstract void Negate();

		public abstract FeatureValue Clone();

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
