using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public interface IInitialNodesPatternSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IQuantifierPatternSyntax<TData, TOffset> Group(string name, Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build);
		IQuantifierPatternSyntax<TData, TOffset> Group(Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build);

		IQuantifierPatternSyntax<TData, TOffset> Annotation(string type, FeatureStruct fs);

		Pattern<TData, TOffset> Value { get; }
	}
}
