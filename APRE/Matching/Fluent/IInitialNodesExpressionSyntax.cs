using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public interface IInitialNodesExpressionSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IQuantifierExpressionSyntax<TData, TOffset> Group(string name, Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build);
		IQuantifierExpressionSyntax<TData, TOffset> Group(Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build);

		IQuantifierExpressionSyntax<TData, TOffset> Annotation(string type, FeatureStruct fs);

		Expression<TData, TOffset> Value { get; }
	}
}
