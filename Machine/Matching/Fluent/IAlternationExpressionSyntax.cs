namespace SIL.Machine.Matching.Fluent
{
	public interface IAlternationExpressionSyntax<TData, TOffset> : INodesExpressionSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IInitialNodesExpressionSyntax<TData, TOffset> Or { get; }
	}
}
