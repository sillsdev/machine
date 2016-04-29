using System.Linq;
using SIL.Machine.FeatureModel;
using SIL.Machine.HermitCrab;
using SIL.Machine.HermitCrab.Tests;

namespace SIL.Machine.Translation.HermitCrab.Tests
{
	public abstract class TranslationHermitCrabTestBase : HermitCrabTestBase
	{
		protected string GetMorphemeId(Morpheme morpheme)
		{
			return morpheme.Gloss;
		}

		protected string GetCategory(FeatureStruct fs)
		{
			SymbolicFeatureValue value;
			if (fs.TryGetValue("pos", out value))
				return value.Values.First().ID;
			return null;
		}
	}
}
