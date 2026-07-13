using System;
using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Runs a fixture against the live C# HermitCrab engine, in-process: loads grammar.xml via
/// <see cref="XmlLanguageLoader"/>, builds a <see cref="Morpher"/>, parses every word in
/// words.txt, and computes the exact same signature <see cref="SignatureFormat"/> that
/// BatchCommand's oracle uses -- byte-identical, since it's the same shared code, not a
/// reimplementation. This is what catches a fixture going stale when the oracle itself changes
/// (the lesson the plan doc cites from LT-22613).
/// </summary>
/// <param name="capabilities">
/// Lets a caller override the reference engine's normal full-capability declaration -- e.g.
/// <c>--capabilities ""</c>, which is how the harness's own capability-filtering skip path gets
/// exercised against the live oracle without needing a second, capability-limited engine on hand.
/// Null (the default) means the full set.
/// </param>
public class SelfCheckEngine(IReadOnlySet<string> capabilities = null) : IEngine
{
    private static readonly IReadOnlySet<string> DefaultCapabilities = new HashSet<string> { "phonology" };

    public string Name => "self-check";

    // The reference C# engine implements every capability the suite currently knows about, unless
    // overridden above.
    public IReadOnlySet<string> Capabilities { get; } = capabilities ?? DefaultCapabilities;

    public List<TsvRow> Run(MaterializedFixture fixture)
    {
        Language language;
        Morpher morpher;
        try
        {
            language = XmlLanguageLoader.Load(fixture.GrammarPath);
            morpher = new Morpher(new TraceManager(), language);
        }
        catch (Exception ex)
        {
            throw new EngineCrashException(
                $"self-check engine crashed loading grammar for fixture '{fixture.Id}': {ex.Message}",
                ex
            );
        }

        string[] words = SignatureFormat.LoadWords(fixture.WordsPath);

        var rows = new List<TsvRow>(words.Length);
        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            try
            {
                // SignatureFormat.ParseOneWord only catches InvalidShapeException itself (yielding
                // "SKIPPED"); anything else it lets propagate is a genuine engine crash, wrapped
                // here the same way grammar-load failures are above.
                (string status, long elapsedMs, string signature) = SignatureFormat.ParseOneWord(morpher, word);
                rows.Add(new TsvRow(i, word, elapsedMs, status, signature));
            }
            catch (Exception ex)
            {
                throw new EngineCrashException(
                    $"self-check engine crashed parsing word '{word}' in fixture '{fixture.Id}': {ex.Message}",
                    ex
                );
            }
        }
        return rows;
    }
}
