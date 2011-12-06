using System;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IDisjunctiveFeatureValueSyntax : IDisjunctiveNegatableFeatureValueSyntax
	{
		IDisjunctiveNegatableFeatureValueSyntax Not { get; }
		IDisjunctiveFeatureStructSyntax EqualToFeatureStruct(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);
		IDisjunctiveFeatureStructSyntax ReferringTo(params Feature[] path);
		IDisjunctiveFeatureStructSyntax ReferringTo(params string[] idPath);
	}
}
