using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    public interface IHCRule
    {
        string Name { get; set; }

        IRule<Word, int> CompileAnalysisRule(Morpher morpher);
        IRule<Word, int> CompileSynthesisRule(Morpher morpher);
    }
}
