using SIL.APRE.Patterns;

namespace SIL.APRE.Rules
{
	public interface IRuleAction<TOffset>
	{
		Annotation<TOffset> Run(Pattern<TOffset> lhs, IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match);
	}
}
