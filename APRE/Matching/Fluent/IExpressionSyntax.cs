using System;

namespace SIL.APRE.Matching.Fluent
{
	public interface IExpressionSyntax<TOffset> : ILeftSideAnchorExpressionSyntax<TOffset>
	{
		IExpressionSyntax<TOffset> MatchAcceptableWhere(Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> acceptable);
	}
}
