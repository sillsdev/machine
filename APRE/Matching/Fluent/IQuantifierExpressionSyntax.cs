namespace SIL.APRE.Matching.Fluent
{
	public interface IQuantifierExpressionSyntax<TOffset> : IAlternationExpressionSyntax<TOffset>
	{
		IAlternationExpressionSyntax<TOffset> ZeroOrMore { get; }
		IAlternationExpressionSyntax<TOffset> LazyZeroOrMore { get; }

		IAlternationExpressionSyntax<TOffset> OneOrMore { get; }
		IAlternationExpressionSyntax<TOffset> LazyOneOrMore { get; }

		IAlternationExpressionSyntax<TOffset> Optional { get; }
		IAlternationExpressionSyntax<TOffset> LazyOptional { get; }

		IAlternationExpressionSyntax<TOffset> Range(int min, int max);
		IAlternationExpressionSyntax<TOffset> LazyRange(int min, int max);
	}
}
