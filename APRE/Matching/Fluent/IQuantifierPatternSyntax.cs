namespace SIL.APRE.Matching.Fluent
{
	public interface IQuantifierPatternSyntax<TOffset> : IAlternationPatternSyntax<TOffset>
	{
		IAlternationPatternSyntax<TOffset> ZeroOrMore { get; }
		IAlternationPatternSyntax<TOffset> LazyZeroOrMore { get; }

		IAlternationPatternSyntax<TOffset> OneOrMore { get; }
		IAlternationPatternSyntax<TOffset> LazyOneOrMore { get; }

		IAlternationPatternSyntax<TOffset> Optional { get; }
		IAlternationPatternSyntax<TOffset> LazyOptional { get; }

		IAlternationPatternSyntax<TOffset> Range(int min, int max);
		IAlternationPatternSyntax<TOffset> LazyRange(int min, int max);
	}
}
