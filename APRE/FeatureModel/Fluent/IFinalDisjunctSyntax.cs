using System;

namespace SIL.APRE.FeatureModel.Fluent
{
	public interface IFinalDisjunctSyntax
	{
		IFinalDisjunctSyntax Or(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);

		Disjunction ToDisjunction();
	}
}
