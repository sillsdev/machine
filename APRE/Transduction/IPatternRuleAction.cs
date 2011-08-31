using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public interface IPatternRuleAction<TOffset>
	{
		bool IsApplicable(IBidirList<Annotation<TOffset>> input);

		Annotation<TOffset> Apply(Pattern<TOffset> lhs, IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match);
	}
}
