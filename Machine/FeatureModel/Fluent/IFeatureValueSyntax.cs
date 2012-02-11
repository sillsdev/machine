using System;

namespace SIL.Machine.FeatureModel.Fluent
{
	public interface IFeatureValueSyntax : INegatableFeatureValueSyntax
	{
		INegatableFeatureValueSyntax Not { get; }
		IFeatureStructSyntax EqualToFeatureStruct(Func<IFeatureStructSyntax, IFeatureStructSyntax> build);
		IFeatureStructSyntax EqualToFeatureStruct(int id, Func<IFeatureStructSyntax, IFeatureStructSyntax> build);
		IFeatureStructSyntax ReferringTo(int id);
	}
}
