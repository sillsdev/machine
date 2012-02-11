using System;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IDisjunctiveFeatureValueSyntax : IDisjunctiveNegatableFeatureValueSyntax
	{
		IDisjunctiveNegatableFeatureValueSyntax Not { get; }
		IDisjunctiveFeatureStructSyntax EqualToFeatureStruct(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);
		IDisjunctiveFeatureStructSyntax EqualToFeatureStruct(int id, Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);
		IDisjunctiveFeatureStructSyntax ReferringTo(int id);
	}
}
