using System.Collections.Generic;

namespace SIL.Machine.Morphology
{
    public interface IMorphologicalAnalyzer
    {
        IEnumerable<WordAnalysis> AnalyzeWord(string word);
    }
}
