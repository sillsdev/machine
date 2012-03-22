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

			var result = new FeatureStruct();
			foreach (Feature feature in fs.Features)
			{
				FeatureValue value = fs.GetValue(feature);
				var childFS = value as FeatureStruct;
				FeatureValue newValue;
				if (childFS != null)
				{
					newValue = HCFeatureSystem.Instance.ContainsFeature(feature) ? childFS.DeepClone() : childFS.AntiFeatureStruct();
				}
				else
				{
					var childSfv = (SimpleFeatureValue) value;
					newValue = HCFeatureSystem.Instance.ContainsFeature(feature) ? childSfv.DeepClone() : childSfv.Negation();
				}
				result.AddValue(feature, newValue);
			}
			return result;
		}

		public static bool IsDirty(this ShapeNode node)
		{
			return ((FeatureSymbol) node.Annotation.FeatureStruct.GetValue(HCFeatureSystem.Modified)) == HCFeatureSystem.Dirty;
		}

		public static void SetDirty(this ShapeNode node, bool dirty)
		{
			node.Annotation.FeatureStruct.AddValue(HCFeatureSystem.Modified, dirty ? HCFeatureSystem.Dirty : HCFeatureSystem.Clean);
		}
	}
}
