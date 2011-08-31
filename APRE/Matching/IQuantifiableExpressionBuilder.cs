namespace SIL.APRE.Matching
{
	public interface IQuantifiableExpressionBuilder<TOffset> : IExpressionBuilder<TOffset>
	{
		IExpressionBuilder<TOffset> ZeroOrMore { get; }

		IExpressionBuilder<TOffset> OneOrMore { get; }

		IExpressionBuilder<TOffset> Optional { get; }

		IExpressionBuilder<TOffset> Range(int min, int max);
	}
}
