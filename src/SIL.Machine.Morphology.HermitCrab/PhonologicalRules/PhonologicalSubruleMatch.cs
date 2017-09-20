using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
	public class PhonologicalSubruleMatch
	{
		public PhonologicalSubruleMatch(IPhonologicalPatternSubruleSpec subruleSpec, Range<ShapeNode> range,
			VariableBindings varBindings)
		{
			SubruleSpec = subruleSpec;
			Range = range;
			VariableBindings = varBindings;
		}

		public IPhonologicalPatternSubruleSpec SubruleSpec { get; }
		public Range<ShapeNode> Range { get; }
		public VariableBindings VariableBindings { get; }
	}
}
