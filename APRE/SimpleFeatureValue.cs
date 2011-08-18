using System.Collections.Generic;

namespace SIL.APRE
{
	public abstract class SimpleFeatureValue : FeatureValue
	{
		protected override bool UnifyCopy(FeatureValue other, bool useDefaults, IDictionary<FeatureValue, FeatureValue> copies,
			IDictionary<string, FeatureValue> varBindings, out FeatureValue output)
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
			if (Forward != null)
				return Forward.Clone(copies);

			FeatureValue copy;
			if (copies.TryGetValue(this, out copy))
				return copy;

			copy = Clone();
			copies[this] = copy;
			return copy;
		}
	}
}
