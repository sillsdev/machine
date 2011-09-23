using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public interface IGroupSyntax<TOffset>
	{
		IQuantifierGroupSyntax<TOffset> Group(string name, Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build);
		IQuantifierGroupSyntax<TOffset> Group(Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build);

		IQuantifierGroupSyntax<TOffset> Annotation(string type, FeatureStruct fs);

		Group<TOffset> Value { get; } 
	}
}
