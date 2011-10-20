namespace SIL.APRE.Matching.Fluent
{
	public interface IQuantifierPatternSyntax<TData, TOffset> : IAlternationPatternSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IAlternationPatternSyntax<TData, TOffset> ZeroOrMore { get; }
		IAlternationPatternSyntax<TData, TOffset> LazyZeroOrMore { get; }

		IAlternationPatternSyntax<TData, TOffset> OneOrMore { get; }
		IAlternationPatternSyntax<TData, TOffset> LazyOneOrMore { get; }

		IAlternationPatternSyntax<TData, TOffset> Optional { get; }
		IAlternationPatternSyntax<TData, TOffset> LazyOptional { get; }

		IAlternationPatternSyntax<TData, TOffset> Range(int min, int max);
		IAlternationPatternSyntax<TData, TOffset> LazyRange(int min, int max);
	}
}
