using System;

namespace SIL.Machine.Matching.Fluent
{
	public interface INodesPatternSyntax<TData, TOffset> : IInitialNodesPatternSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		INodesPatternSyntax<TData, TOffset> Expression(Func<IExpressionSyntax<TData, TOffset>, IExpressionSyntax<TData, TOffset>> build);
		INodesPatternSyntax<TData, TOffset> Expression(string name, Func<IExpressionSyntax<TData, TOffset>, IExpressionSyntax<TData, TOffset>> build);
	}
}
