using System;

namespace SIL.APRE.Matching
{
	public interface IPatternBuilder<TOffset>
	{
		IPatternBuilder<TOffset> MatchLeftToRight { get; }
		IPatternBuilder<TOffset> MatchRightToLeft { get; }

		IPatternBuilder<TOffset> AllowWhere(Func<Annotation<TOffset>, bool> filter);

		IPatternBuilder<TOffset> Expression(Func<IExpressionBuilder<TOffset>, IExpressionBuilder<TOffset>> build);
		IPatternBuilder<TOffset> Expression(string name, Func<IExpressionBuilder<TOffset>, IExpressionBuilder<TOffset>> build);

		Pattern<TOffset> Value { get; } 
	}
}
