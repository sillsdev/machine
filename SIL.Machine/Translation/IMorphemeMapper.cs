using SIL.Machine.Morphology;

namespace SIL.Machine.Translation
{
	public interface IMorphemeMapper
	{
		bool TryGetTargetMorpheme(Morpheme sourceMorpheme, out Morpheme targetMorpheme);
	}
}
