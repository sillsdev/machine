using System;

namespace SIL.APRE.FeatureModel.Fluent
{
	public interface IFeatureValueSyntax : INegatableFeatureValueSyntax
	{
		INegatableFeatureValueSyntax Not { get; }
		IFeatureStructSyntax EqualToFeatureStruct(Func<IFeatureStructSyntax, IFeatureStructSyntax> build);
		IFeatureStructSyntax ReferringTo(params Feature[] path);
		IFeatureStructSyntax ReferringTo(params string[] idPath);
	}
}
