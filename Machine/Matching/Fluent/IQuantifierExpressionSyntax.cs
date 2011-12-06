namespace SIL.Machine.Matching.Fluent
{
	public interface IQuantifierExpressionSyntax<TData, TOffset> : IAlternationExpressionSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IAlternationExpressionSyntax<TData, TOffset> ZeroOrMore { get; }
		IAlternationExpressionSyntax<TData, TOffset> LazyZeroOrMore { get; }

		IAlternationExpressionSyntax<TData, TOffset> OneOrMore { get; }
		IAlternationExpressionSyntax<TData, TOffset> LazyOneOrMore { get; }

		IAlternationExpressionSyntax<TData, TOffset> Optional { get; }
		IAlternationExpressionSyntax<TData, TOffset> LazyOptional { get; }

		IAlternationExpressionSyntax<TData, TOffset> Range(int min, int max);
		IAlternationExpressionSyntax<TData, TOffset> LazyRange(int min, int max);
	}
}
