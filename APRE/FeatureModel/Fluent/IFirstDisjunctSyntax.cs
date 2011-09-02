using System;

namespace SIL.APRE.FeatureModel.Fluent
{
	public interface IFirstDisjunctSyntax
	{
		ISecondDisjunctSyntax With(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);
	}
}
