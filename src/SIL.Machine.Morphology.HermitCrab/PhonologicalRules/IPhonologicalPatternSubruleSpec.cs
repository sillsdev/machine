using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
	public interface IPhonologicalPatternSubruleSpec
	{
		Matcher<Word, ShapeNode> LeftEnvironmentMatcher { get; }
		Matcher<Word, ShapeNode> RightEnvironmentMatcher { get; }

		bool IsApplicable(Word input);
		void ApplyRhs(Match<Word, ShapeNode> targetMatch, Range<ShapeNode> range, VariableBindings varBindings);
	}
}
