using System;
using SIL.Collections;

namespace SIL.Machine.Matching.Fluent
{
	public interface IPatternSyntax<TData, TOffset> : INodesPatternSyntax<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		INodesPatternSyntax<TData, TOffset> MatchAcceptableWhere(Func<Match<TData, TOffset>, bool> acceptable);
	}
}
