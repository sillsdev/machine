using System;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IFinalDisjunctSyntax
	{
		IFinalDisjunctSyntax Or(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);

		Disjunction ToDisjunction();
	}
}
