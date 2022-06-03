using SIL.Machine.Morphology;

namespace SIL.Machine.Translation
{
    public interface IMorphemeMapper
    {
        bool TryGetTargetMorpheme(IMorpheme sourceMorpheme, out IMorpheme targetMorpheme);
    }
}
