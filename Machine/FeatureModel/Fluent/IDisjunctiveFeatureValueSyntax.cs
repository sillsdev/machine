using System;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IDisjunctiveFeatureValueSyntax : IDisjunctiveNegatableFeatureValueSyntax
	{
		IDisjunctiveNegatableFeatureValueSyntax Not { get; }
		IDisjunctiveFeatureStructSyntax EqualTo(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);
		IDisjunctiveFeatureStructSyntax EqualTo(FeatureStruct fs);
		IDisjunctiveFeatureStructSyntax EqualTo(int id, Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build);
		IDisjunctiveFeatureStructSyntax EqualTo(int id, FeatureStruct fs);
		IDisjunctiveFeatureStructSyntax ReferringTo(int id);
	}
}
