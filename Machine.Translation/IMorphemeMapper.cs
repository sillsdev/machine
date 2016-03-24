namespace SIL.Machine.Translation
{
	public interface IMorphemeMapper
	{
		bool TryGetTargetMorpheme(MorphemeInfo sourceMorpheme, out MorphemeInfo targetMorpheme);
	}
}
