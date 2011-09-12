namespace SIL.APRE.Matching.Fluent
{
	public interface ILeftSideAnchorPatternSyntax<TOffset> : INodesPatternSyntax<TOffset>
	{
		INodesPatternSyntax<TOffset> LeftSideOfInput { get; }
	}
}
