using SIL.Collections;

namespace SIL.Machine.Matching.Fluent
{
	public interface IAlternationPatternSyntax<TData, TOffset> : INodesPatternSyntax<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		IInitialNodesPatternSyntax<TData, TOffset> Or { get; }
	}
}
