namespace SIL.APRE.Matching.Fluent
{
	public interface IFinalExpressionSyntax<TOffset>
	{
		Expression<TOffset> Value { get; }
	}
}
