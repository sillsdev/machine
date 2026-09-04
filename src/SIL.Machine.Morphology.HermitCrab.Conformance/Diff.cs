using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>Per-word outcome of diffing one fixture's actual output against its expected.tsv.</summary>
public class WordResult
{
    public string Word = "";
    public bool Passed;
    public string Detail = "";
}

public class FixtureDiff
{
    public List<WordResult> WordResults { get; } = new();
    public bool AllPassed => WordResults.All(w => w.Passed);
}

/// <summary>
/// Word/status/signature-only diffing between two batch-TSV row sets, per conformance/PROTOCOL.md
/// section 4. Rows are matched by word (idx/ms are never compared); a signature's ";"-separated
/// entries are compared as a multiset via <see cref="SignatureTsv"/>.
/// </summary>
public static class Diff
{
    public static FixtureDiff Compare(List<TsvRow> expected, List<TsvRow> actual)
    {
        var diff = new FixtureDiff();
        Dictionary<string, List<TsvRow>> expByWord = GroupByWord(expected);
        Dictionary<string, List<TsvRow>> actByWord = GroupByWord(actual);

        IEnumerable<string> allWords = expByWord
            .Keys.Union(actByWord.Keys, StringComparer.Ordinal)
            .OrderBy(w => w, StringComparer.Ordinal);

        foreach (string word in allWords)
        {
            bool hasExpected = expByWord.TryGetValue(word, out List<TsvRow> expRows);
            bool hasActual = actByWord.TryGetValue(word, out List<TsvRow> actRows);

            if (!hasExpected)
            {
                diff.WordResults.Add(
                    new WordResult
                    {
                        Word = word,
                        Passed = false,
                        Detail = "present in adapter output but not in expected.tsv",
                    }
                );
                continue;
            }
            if (!hasActual)
            {
                diff.WordResults.Add(
                    new WordResult
                    {
                        Word = word,
                        Passed = false,
                        Detail = "missing from adapter/self-check output",
                    }
                );
                continue;
            }

            List<string> expCanon = Canonicalize(expRows);
            List<string> actCanon = Canonicalize(actRows);
            bool passed = expCanon.SequenceEqual(actCanon, StringComparer.Ordinal);
            diff.WordResults.Add(
                new WordResult
                {
                    Word = word,
                    Passed = passed,
                    Detail = passed
                        ? "ok"
                        : $"expected [{string.Join(" | ", expCanon)}] got [{string.Join(" | ", actCanon)}]",
                }
            );
        }

        return diff;
    }

    private static Dictionary<string, List<TsvRow>> GroupByWord(List<TsvRow> rows)
    {
        var byWord = new Dictionary<string, List<TsvRow>>();
        foreach (TsvRow row in rows)
        {
            if (!byWord.TryGetValue(row.Word, out List<TsvRow> list))
            {
                list = new List<TsvRow>();
                byWord[row.Word] = list;
            }
            list.Add(row);
        }
        return byWord;
    }

    // One canonical "status|sorted-signature-entries" string per row, sorted across the word's
    // rows (usually just one) so occurrence order/duplication is handled as a multiset.
    private static List<string> Canonicalize(List<TsvRow> rows)
    {
        return rows.Select(r => r.Status + "|" + string.Join(";", SignatureTsv.SplitSignature(r.Signature)))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
    }
}
