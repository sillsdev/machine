using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab;

internal static class SignatureFormat
{
    /// <summary>Reads a word list file: one word per line, trimmed, blank lines dropped.</summary>
    public static string[] LoadWords(string path)
    {
        return File.ReadAllLines(path).Select(w => w.Trim()).Where(w => w.Length > 0).ToArray();
    }

    public static (string status, long elapsedMs, string signature) ParseOneWord(Morpher morpher, string word)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Word[] results = morpher.ParseWord(word, out _).ToArray();
            sw.Stop();
            return ("ok", sw.ElapsedMilliseconds, BuildSignature(results));
        }
        catch (InvalidShapeException)
        {
            return ("SKIPPED", 0, "-");
        }
    }

    /// <summary>
    /// Builds the order-independent, sorted, semicolon-joined signature for a set of parse
    /// results: one <c>morph+morph|shape</c> entry per distinct analysis, or "-" if empty.
    /// </summary>
    public static string BuildSignature(IEnumerable<Word> results)
    {
        List<string> signatures = results
            .Select(w =>
                string.Join("+", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Id))
                + "|"
                + w.Shape.ToRegexString(w.Stratum.CharacterDefinitionTable, true)
            )
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
        return signatures.Count == 0 ? "-" : string.Join(";", signatures);
    }
}
