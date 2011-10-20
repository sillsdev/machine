using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public interface IGroupSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IQuantifierGroupSyntax<TData, TOffset> Group(string name, Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build);
		IQuantifierGroupSyntax<TData, TOffset> Group(Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build);

		IQuantifierGroupSyntax<TData, TOffset> Annotation(string type, FeatureStruct fs);

		Group<TData, TOffset> Value { get; } 
	}
}
