using System;

namespace SIL.APRE.FeatureModel
{
	public interface ISecondDisjunctBuilder
	{
		IFinalDisjunctBuilder Or(Func<IDisjunctiveFeatureStructBuilder, IDisjunctiveFeatureStructBuilder> build);
	}
}
