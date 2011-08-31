using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching
{
	public interface IGroupBuilder<TOffset>
	{
		IGroupBuilder<TOffset> Or { get; }

		IQuantifiableGroupBuilder<TOffset> Group(string name, Func<IGroupBuilder<TOffset>, IGroupBuilder<TOffset>> build);
		IQuantifiableGroupBuilder<TOffset> Group(Func<IGroupBuilder<TOffset>, IGroupBuilder<TOffset>> build);

		IQuantifiableGroupBuilder<TOffset> Annotation(FeatureStruct fs);

		Group<TOffset> Value { get; } 
	}
}
