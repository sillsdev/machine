using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public interface IInitialNodesPatternSyntax<TOffset>
	{
		IQuantifierPatternSyntax<TOffset> Group(string name, Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build);
		IQuantifierPatternSyntax<TOffset> Group(Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build);

		IQuantifierPatternSyntax<TOffset> Annotation(string type, FeatureStruct fs);

		IFinalPatternSyntax<TOffset> RightSideOfInput { get; } 
	}
}
