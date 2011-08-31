using System;

namespace SIL.APRE.FeatureModel
{
	public interface IFinalDisjunctBuilder
	{
		IFinalDisjunctBuilder Or(Func<IDisjunctiveFeatureStructBuilder, IDisjunctiveFeatureStructBuilder> build);

		Disjunction ToDisjunction();
	}
}
