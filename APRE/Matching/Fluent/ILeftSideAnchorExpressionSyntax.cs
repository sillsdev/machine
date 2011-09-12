namespace SIL.APRE.Matching.Fluent
{
	public interface ILeftSideAnchorExpressionSyntax<TOffset> : INodesExpressionSyntax<TOffset>
	{
		INodesExpressionSyntax<TOffset> LeftSideOfInput { get; }
	}
}
