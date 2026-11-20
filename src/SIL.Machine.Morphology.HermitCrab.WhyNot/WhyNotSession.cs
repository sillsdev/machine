using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab.WhyNot;

/// <summary>
/// Compiles the grammar once and answers many queries. Not safe for concurrent use.
/// </summary>
public class WhyNotSession
{
    private readonly CapturingTraceManager _trace = new CapturingTraceManager();
    private readonly Morpher _morpher;

    public WhyNotSession(Language language)
    {
        _morpher = new Morpher(_trace, language, 1);
    }

    public WhyNotResult Analyze(string word, IReadOnlyList<ExpectedMorph> expected)
    {
        _trace.Clear();
        return WhyNotAnalyzer.Analyze(_morpher, _trace, word, expected);
    }
}
