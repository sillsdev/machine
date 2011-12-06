using System;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IFirstDisjunctSyntax
	{
		ISecondDisjunctSyntax With(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);
	}
}
