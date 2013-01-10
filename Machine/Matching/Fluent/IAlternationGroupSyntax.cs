using SIL.Collections;

namespace SIL.Machine.Matching.Fluent
{
	public interface IAlternationGroupSyntax<TData, TOffset> : IGroupSyntax<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		IGroupSyntax<TData, TOffset> Or { get; }
	}
}
