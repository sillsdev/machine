namespace SIL.APRE.Matching.Fluent
{
	public interface IQuantifierPatternSyntax<TOffset> : IAlternationPatternSyntax<TOffset>
	{
		IAlternationPatternSyntax<TOffset> ZeroOrMore { get; }

		IAlternationPatternSyntax<TOffset> OneOrMore { get; }

		IAlternationPatternSyntax<TOffset> Optional { get; }

		IAlternationPatternSyntax<TOffset> Range(int min, int max);
	}
}
