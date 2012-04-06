using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using System.Linq;

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

		public static void CheckUninstantiatedFeatures(this VariableBindings varBindings)
		{
			foreach (SymbolicFeatureValue sfv in varBindings.Values.OfType<SymbolicFeatureValue>())
			{
				if (sfv.Feature.DefaultValue.ValueEquals(sfv))
					throw new MorphException(MorphErrorCode.UninstantiatedFeature) {Data = {{"feature", sfv.Feature.ID}}};
			}
		}

		private static readonly IEqualityComparer<ShapeNode> NodeComparer = new ProjectionEqualityComparer<ShapeNode, FeatureStruct>(node => node.Annotation.FeatureStruct,
			FreezableEqualityComparer<FeatureStruct>.Instance);
		public static bool Duplicates(this Shape x, Shape y)
		{
			return x.Where(n => !n.Annotation.Optional).SequenceEqual(y.Where(n => !n.Annotation.Optional), NodeComparer);
		}

		public static IEnumerable<Word> RemoveDuplicates(this IEnumerable<Word> words)
		{
			var output = new List<Word>();
			foreach (Word word in words)
			{
				// check to see if this is a duplicate of another output analysis, this is not strictly necessary, but
				// it helps to reduce the search space
				bool add = true;
				for (int i = 0; i < output.Count; i++)
				{
					if (word.Shape.Duplicates(output[i].Shape))
					{
						if (word.Shape.Count > output[i].Shape.Count)
							// if this is a duplicate and it is longer, then use this analysis and remove the previous one
							output.RemoveAt(i);
						else
							// if it is shorter, then do not add it to the output list
							add = false;
						break;
					}
				}

				if (add)
					output.Add(word);
			}
			return output;
		}
	}
}
