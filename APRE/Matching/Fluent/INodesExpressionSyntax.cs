using System;

namespace SIL.APRE.Matching.Fluent
{
	public interface INodesExpressionSyntax<TData, TOffset> : IInitialNodesExpressionSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		INodesExpressionSyntax<TData, TOffset> Expression(Func<IExpressionSyntax<TData, TOffset>, IExpressionSyntax<TData, TOffset>> build);
		INodesExpressionSyntax<TData, TOffset> Expression(string name, Func<IExpressionSyntax<TData, TOffset>, IExpressionSyntax<TData, TOffset>> build);
	}
}
