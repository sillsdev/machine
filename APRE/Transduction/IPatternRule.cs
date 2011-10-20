using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public enum ApplicationMode
	{
		Single,
		Multiple,
		Iterative,
		Simultaneous
	}

	public interface IPatternRule<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>
	{
		Pattern<TData, TOffset> Lhs { get; }

		ApplicationMode ApplicationMode { get; }

		void Compile();

		Annotation<TOffset> ApplyRhs(TData input, PatternMatch<TOffset> match, out TData output);
	}
}
