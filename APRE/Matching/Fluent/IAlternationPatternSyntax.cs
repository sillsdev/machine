namespace SIL.APRE.Matching.Fluent
{
	public interface IAlternationPatternSyntax<TData, TOffset> : INodesPatternSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IInitialNodesPatternSyntax<TData, TOffset> Or { get; }
	}
}
