using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab
{
	public static class Extensions
	{
		public static FeatureSymbol Type(this Annotation<ShapeNode> ann)
		{
			return (FeatureSymbol) ann.FeatureStruct.GetValue(HCFeatureSystem.Type);
		}

		public static FeatureSymbol Type(this Constraint<Word, ShapeNode> constraint)
		{
			return (FeatureSymbol) constraint.FeatureStruct.GetValue(HCFeatureSystem.Type);
		}

		public static FeatureStruct AntiFeatureStruct(this FeatureStruct fs)
		{
			// TODO: handle reentrancy properly

			IReadOnlySet<Feature> hcFeatures = HCFeatureSystem.Instance.Features;
			var result = new FeatureStruct();
			foreach (Feature feature in fs.Features)
			{
				if (hcFeatures.Contains(feature))
					continue;

				FeatureValue value = fs.GetValue(feature);
				var childFS = value as FeatureStruct;
				FeatureValue newValue;
				if (childFS != null)
					newValue = childFS.AntiFeatureStruct();
				else
					newValue = ((SimpleFeatureValue)value).Negation();
				result.AddValue(feature, newValue);
			}
			return result;
		}
	}
}
