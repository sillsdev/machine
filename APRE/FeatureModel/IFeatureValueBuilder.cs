using System;

namespace SIL.APRE.FeatureModel
{
	public interface IFeatureValueBuilder : INegatableFeatureValueBuilder
	{
		INegatableFeatureValueBuilder Not { get; }
		IFeatureStructBuilder EqualToFeatureStruct(Func<IFeatureStructBuilder, IFeatureStructBuilder> build);
		IFeatureStructBuilder ReferringTo(params Feature[] path);
		IFeatureStructBuilder ReferringTo(params string[] idPath);
	}
}
