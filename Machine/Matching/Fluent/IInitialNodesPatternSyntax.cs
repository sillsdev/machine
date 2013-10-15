using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Matching.Fluent
{
	public interface IInitialNodesPatternSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IQuantifierPatternSyntax<TData, TOffset> Group(string name, Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build);
		IQuantifierPatternSyntax<TData, TOffset> Group(Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build);

		IQuantifierPatternSyntax<TData, TOffset> Annotation(FeatureStruct fs);

		Pattern<TData, TOffset> Value { get; }
	}
}
