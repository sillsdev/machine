namespace SIL.Machine.Translation
{
	public interface IMorphemeMapper
	{
		Morpheme GetTargetMorpheme(Morpheme sourceMorpheme);
	}
}
