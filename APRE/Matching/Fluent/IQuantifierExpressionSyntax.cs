namespace SIL.APRE.Matching.Fluent
{
	public interface IQuantifierExpressionSyntax<TOffset> : IAlternationExpressionSyntax<TOffset>
	{
		IAlternationExpressionSyntax<TOffset> ZeroOrMore { get; }

		IAlternationExpressionSyntax<TOffset> OneOrMore { get; }

		IAlternationExpressionSyntax<TOffset> Optional { get; }

		IAlternationExpressionSyntax<TOffset> Range(int min, int max);
	}
}
