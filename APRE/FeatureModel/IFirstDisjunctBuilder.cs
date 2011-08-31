using System;

namespace SIL.APRE.FeatureModel
{
	public interface IFirstDisjunctBuilder
	{
		ISecondDisjunctBuilder With(Func<IDisjunctiveFeatureStructBuilder, IDisjunctiveFeatureStructBuilder> build);
	}
}
