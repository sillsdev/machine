using System;

namespace SIL.APRE.Matching.Fluent
{
	public interface INodesExpressionSyntax<TOffset> : IInitialNodesExpressionSyntax<TOffset>
	{
		INodesExpressionSyntax<TOffset> Expression(Func<IExpressionSyntax<TOffset>, IExpressionSyntax<TOffset>> build);
		INodesExpressionSyntax<TOffset> Expression(string name, Func<IExpressionSyntax<TOffset>, IExpressionSyntax<TOffset>> build);

		Expression<TOffset> Value { get; }
	}
}
