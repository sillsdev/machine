using System;

namespace SIL.Machine.Matching.Fluent
{
	public interface IExpressionSyntax<TData, TOffset> : INodesExpressionSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IExpressionSyntax<TData, TOffset> MatchAcceptableWhere(Func<TData, PatternMatch<TOffset>, bool> acceptable);
	}
}
