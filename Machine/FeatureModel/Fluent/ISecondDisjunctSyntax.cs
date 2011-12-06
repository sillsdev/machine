using System;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface ISecondDisjunctSyntax
	{
		IFinalDisjunctSyntax Or(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);
	}
}
