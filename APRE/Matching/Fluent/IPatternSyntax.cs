using System;

namespace SIL.APRE.Matching.Fluent
{
	public interface IPatternSyntax<TData, TOffset> : INodesPatternSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IPatternSyntax<TData, TOffset> MatchLeftToRight { get; }
		IPatternSyntax<TData, TOffset> MatchRightToLeft { get; }

		IPatternSyntax<TData, TOffset> AnnotationsAllowableWhere(Func<Annotation<TOffset>, bool> filter);
	}
}
