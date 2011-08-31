using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public interface IPatternRule<TOffset> : IRule<TOffset>
	{
		Pattern<TOffset> Lhs { get; }

		bool Simultaneous { get; }

		void Compile();

		Annotation<TOffset> ApplyRhs(IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match);
	}
}
