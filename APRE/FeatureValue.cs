using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	public enum FeatureValueType { Symbol, Complex, String, Variable };

	public abstract class FeatureValue : ICloneable
	{
		public abstract FeatureValueType Type { get; }

		internal FeatureValue Forward { get; set; }

		public abstract FeatureValue Clone();

		object ICloneable.Clone()
		{
			return Clone();
		}

		internal abstract bool Negation(out FeatureValue output);
		internal abstract bool IsUnifiable(FeatureValue other, bool useDefaults, IDictionary<string, FeatureValue> varBindings);
		internal abstract bool DestructiveUnify(FeatureValue other, bool useDefaults, bool preserveInput,
			IDictionary<FeatureValue, FeatureValue> copies, IDictionary<string, FeatureValue> varBindings);
		internal abstract FeatureValue Clone(IDictionary<FeatureValue, FeatureValue> copies);

		internal bool Unify(FeatureValue other, bool useDefaults, IDictionary<FeatureValue, FeatureValue> copies,
			IDictionary<string, FeatureValue> varBindings, out FeatureValue output)
		{
			if (Forward != null)
				return Forward.Unify(other, useDefaults, copies, varBindings, out output);

			other = GetValue(other);

			FeatureValue fv1;
			if (!copies.TryGetValue(this, out fv1))
				fv1 = null;
			FeatureValue fv2;
			if (!copies.TryGetValue(other, out fv2))
				fv2 = null;

			if (fv1 == null && fv2 == null)
			{
				if (Type != FeatureValueType.Variable && other.Type == FeatureValueType.Variable)
				{
					if (!other.UnifyCopy(this, useDefaults, copies, varBindings, out output))
					{
						output = null;
						return false;
					}
				}
				else
				{
					if (!UnifyCopy(other, useDefaults, copies, varBindings, out output))
					{
						output = null;
						return false;
					}
				}
			}
			else if (fv1 != null && fv2 != null)
			{
				if (fv1.Type != FeatureValueType.Variable && fv2.Type == FeatureValueType.Variable)
				{
					fv2.DestructiveUnify(fv1, useDefaults, false, copies, varBindings);
					output = fv2;
				}
				else
				{
					fv1.DestructiveUnify(fv2, useDefaults, false, copies, varBindings);
					output = fv1;
				}
			}
			else if (fv1 != null)
			{
				if (fv1.Type != FeatureValueType.Variable && other.Type == FeatureValueType.Variable)
				{
					if (!other.UnifyCopy(fv1, useDefaults, copies, varBindings, out output))
					{
						output = null;
						return false;
					}
				}
				else
				{
					fv1.DestructiveUnify(other, useDefaults, true, copies, varBindings);
					output = fv1;
				}
			}
			else
			{
				if (fv2.Type != FeatureValueType.Variable && Type == FeatureValueType.Variable)
				{
					if (!UnifyCopy(fv2, useDefaults, copies, varBindings, out output))
					{
						output = null;
						return false;
					}
				}
				else
				{
					fv2.DestructiveUnify(this, useDefaults, true, copies, varBindings);
					output = fv2;
				}
			}

			return true;
		}

		protected abstract bool UnifyCopy(FeatureValue other, bool useDefaults, IDictionary<FeatureValue, FeatureValue> copies,
			IDictionary<string, FeatureValue> varBindings, out FeatureValue output);

		protected static bool GetValue<T>(FeatureValue value, out T actualValue) where T : FeatureValue
		{
			value = GetValue(value);

			if (!(value is T))
			{
				actualValue = null;
				return false;
			}

			actualValue = (T) value;
			return true;
		}

		protected static T GetValue<T>(T value) where T : FeatureValue
		{
			while (value.Forward != null)
				value = (T) value.Forward;
			return value;
		}
	}
}
