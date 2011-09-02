using System.Collections.Generic;

namespace SIL.APRE.FeatureModel
{
	public abstract class SimpleFeatureValue : FeatureValue
	{
		internal override bool IsDefiniteUnifiable(FeatureValue other, bool useDefaults, VariableBindings varBindings)
		{
			other = Dereference(other);
			var otherVfv = other as VariableFeatureValue;
			if (otherVfv != null && !(this is VariableFeatureValue))
			{
				FeatureValue binding;
				if (varBindings.TryGetValue(otherVfv.Name, out binding))
					return IsValuesUnifiable(binding, !otherVfv.Agree);
				return true;
			}

			return IsValuesUnifiable(other, false);
		}

		protected abstract bool IsValuesUnifiable(FeatureValue other, bool negate);

		internal override bool DestructiveUnify(FeatureValue other, bool useDefaults, bool preserveInput, IDictionary<FeatureValue, FeatureValue> copies, VariableBindings varBindings)
		{
			if (!IsDefiniteUnifiable(other, useDefaults, varBindings))
				return false;

			other = Dereference(other);
			if (preserveInput)
			{
				if (copies != null)
					copies[other] = this;
			}
			else
			{
				other.Forward = this;
			}

			var otherVfv = other as VariableFeatureValue;
			if (otherVfv != null && !(this is VariableFeatureValue))
			{
				
				FeatureValue binding;
				if (varBindings.TryGetValue(otherVfv.Name, out binding))
					UnifyValues(binding, !otherVfv.Agree);
				if (otherVfv.Agree)
					binding = Clone();
				else
					Negation(out binding);
				varBindings[otherVfv.Name] = binding;
			}
			else
			{
				UnifyValues(other, false);
			}

			return true;
		}

		protected abstract void UnifyValues(FeatureValue other, bool negate);

		protected override bool NondestructiveUnify(FeatureValue other, bool useDefaults, IDictionary<FeatureValue, FeatureValue> copies,
			VariableBindings varBindings, out FeatureValue output)
		{
			FeatureValue copy = Clone();
			copies[this] = copy;
			copies[other] = copy;
			if (!copy.DestructiveUnify(other, useDefaults, true, copies, varBindings))
			{
				output = null;
				return false;
			}
			output = copy;
			return true;
		}

		internal override FeatureValue Clone(IDictionary<FeatureValue, FeatureValue> copies)
		{
			FeatureValue copy;
			if (copies.TryGetValue(this, out copy))
				return copy;

			copy = Clone();
			copies[this] = copy;
			return copy;
		}
	}
}
