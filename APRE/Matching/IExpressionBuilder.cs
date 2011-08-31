using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching
{
	public interface IExpressionBuilder<TOffset>
	{
		IExpressionBuilder<TOffset> Or { get; }

		IQuantifiableExpressionBuilder<TOffset> Group(string name, Func<IGroupBuilder<TOffset>, IGroupBuilder<TOffset>> build);
		IQuantifiableExpressionBuilder<TOffset> Group(Func<IGroupBuilder<TOffset>, IGroupBuilder<TOffset>> build);

		IQuantifiableExpressionBuilder<TOffset> Annotation(FeatureStruct fs);

		Expression<TOffset> Value { get; } 
	}
}
