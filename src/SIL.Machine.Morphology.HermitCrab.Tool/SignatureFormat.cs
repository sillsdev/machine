using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// The `batch` word-list loading, per-word parse, and signature algorithm, factored out of
/// <see cref="BatchCommand"/> so the conformance harness's self-check mode
/// (<c>SIL.Machine.Morphology.HermitCrab.Conformance</c>) computes byte-identical results from the
/// same code, rather than a second copy that could silently drift from the oracle. See
/// <c>conformance/PROTOCOL.md</c> sections 2-3 for the documented, engine-agnostic description of
/// the TSV format and this signature algorithm.
/// </summary>
public static class SignatureFormat
{
    /// <summary>Reads a word list file: one word per line, trimmed, blank lines dropped.</summary>
    public static string[] LoadWords(string path)
    {
        return File.ReadAllLines(path).Select(w => w.Trim()).Where(w => w.Length > 0).ToArray();
    }

    /// <summary>
    /// Parses one word, returning its batch-TSV status/elapsed-ms/signature triple. Only
    /// <see cref="InvalidShapeException"/> is caught here (yielding <c>("SKIPPED", 0, "-")</c>, per
    /// <c>conformance/PROTOCOL.md</c> section 2) -- any other exception propagates, since that's a
    /// genuine engine crash, not a normal per-word outcome.
    /// </summary>
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
