using System;

namespace SIL.APRE
{
	public enum FeatureValueType { Symbol, Complex, String, Variable };

	public abstract class FeatureValue : ICloneable
	{
		public abstract FeatureValueType Type { get; }

		public abstract bool IsAmbiguous { get; }

		public abstract bool Unify(FeatureValue other, out FeatureValue output, bool useDefaults);
		public abstract bool Matches(FeatureValue other);
		public abstract bool IsUnifiable(FeatureValue other);
		public abstract bool UnifyWith(FeatureValue other, bool useDefaults);

		public abstract void Instantiate(FeatureValue other);
		public abstract void Uninstantiate(FeatureValue other);
		public abstract void UninstantiateAll();

		public abstract FeatureValue Clone();

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
