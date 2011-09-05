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
					return Overlaps(binding, !otherVfv.Agree);
				return true;
			}

			return Overlaps(other, false);
		}

		internal override void MergeValues(FeatureValue other, VariableBindings varBindings)
		{
			other = Dereference(other);
			var otherVfv = other as VariableFeatureValue;
			if (otherVfv != null && !(this is VariableFeatureValue))
			{
				FeatureValue binding;
				if (varBindings.TryGetValue(otherVfv.Name, out binding))
					UnionWith(binding, !otherVfv.Agree);
			}
			else
			{
				UnionWith(other, false);
			}
		}

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
					IntersectWith(binding, !otherVfv.Agree);
				if (otherVfv.Agree)
					binding = Clone();
				else
					Negation(out binding);
				varBindings[otherVfv.Name] = binding;
			}
			else
			{
				IntersectWith(other, false);
			}

			return true;
		}

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

		protected abstract bool Overlaps(FeatureValue other, bool negate);
		protected abstract void IntersectWith(FeatureValue other, bool negate);
		protected abstract void UnionWith(FeatureValue other, bool negate);
	}
}
