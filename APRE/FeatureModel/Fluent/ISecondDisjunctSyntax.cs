using System;

namespace SIL.APRE.FeatureModel.Fluent
{
	public interface ISecondDisjunctSyntax
	{
		IFinalDisjunctSyntax Or(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);
	}
}
