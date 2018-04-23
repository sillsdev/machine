using System;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IFeatureValueSyntax : INegatableFeatureValueSyntax
	{
		INegatableFeatureValueSyntax Not { get; }
		IFeatureStructSyntax EqualTo(Func<IFeatureStructSyntax, IFeatureStructSyntax> build);
		IFeatureStructSyntax EqualTo(FeatureStruct fs);
		IFeatureStructSyntax EqualTo(int id, Func<IFeatureStructSyntax, IFeatureStructSyntax> build);
		IFeatureStructSyntax EqualTo(int id, FeatureStruct fs);
		IFeatureStructSyntax ReferringTo(int id);
	}
}
