using System;
using SIL.Machine.Annotations;

namespace SIL.Machine.Matching.Fluent
{
	public interface INodesPatternSyntax<TData, TOffset> : IInitialNodesPatternSyntax<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		INodesPatternSyntax<TData, TOffset> Subpattern(Func<IPatternSyntax<TData, TOffset>, IInitialNodesPatternSyntax<TData, TOffset>> build);
		INodesPatternSyntax<TData, TOffset> Subpattern(string name, Func<IPatternSyntax<TData, TOffset>, IInitialNodesPatternSyntax<TData, TOffset>> build);
	}
}
