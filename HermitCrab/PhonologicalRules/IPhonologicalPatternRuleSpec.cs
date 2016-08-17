using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.PhonologicalRules
{
	public interface IPhonologicalPatternRuleSpec
	{
		Pattern<Word, ShapeNode> Pattern { get; }
		bool MatchSubrule(PhonologicalPatternRule rule, Match<Word, ShapeNode> match, out PhonologicalSubruleMatch subruleMatch);
	}
}
