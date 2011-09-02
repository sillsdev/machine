namespace SIL.APRE.Matching.Fluent
{
	public interface IAlternationPatternSyntax<TOffset> : INodesPatternSyntax<TOffset>
	{
		IInitialNodesPatternSyntax<TOffset> Or { get; }
	}
}
