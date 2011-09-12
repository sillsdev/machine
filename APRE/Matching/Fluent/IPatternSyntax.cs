using System;

namespace SIL.APRE.Matching.Fluent
{
	public interface IPatternSyntax<TOffset> : ILeftSideAnchorPatternSyntax<TOffset>
	{
		IPatternSyntax<TOffset> MatchLeftToRight { get; }
		IPatternSyntax<TOffset> MatchRightToLeft { get; }

		IPatternSyntax<TOffset> AnnotationsAllowableWhere(Func<Annotation<TOffset>, bool> filter);
	}
}
