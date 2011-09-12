using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public interface IInitialNodesExpressionSyntax<TOffset>
	{
		IQuantifierExpressionSyntax<TOffset> Group(string name, Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build);
		IQuantifierExpressionSyntax<TOffset> Group(Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build);

		IQuantifierExpressionSyntax<TOffset> Annotation(FeatureStruct fs);

		IFinalExpressionSyntax<TOffset> RightSideOfInput { get; } 
	}
}
