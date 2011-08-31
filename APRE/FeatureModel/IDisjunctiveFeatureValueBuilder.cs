using System;

namespace SIL.APRE.FeatureModel
{
	public interface IDisjunctiveFeatureValueBuilder : IDisjunctiveNegatableFeatureValueBuilder
	{
		IDisjunctiveNegatableFeatureValueBuilder Not { get; }
		IDisjunctiveFeatureStructBuilder EqualToFeatureStruct(Func<IDisjunctiveFeatureStructBuilder, IDisjunctiveFeatureStructBuilder> build);
		IDisjunctiveFeatureStructBuilder ReferringTo(params Feature[] path);
		IDisjunctiveFeatureStructBuilder ReferringTo(params string[] idPath);
	}
}
