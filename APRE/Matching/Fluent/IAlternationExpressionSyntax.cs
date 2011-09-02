namespace SIL.APRE.Matching.Fluent
{
	public interface IAlternationExpressionSyntax<TOffset> : INodesExpressionSyntax<TOffset>
	{
		IInitialNodesExpressionSyntax<TOffset> Or { get; }
	}
}
