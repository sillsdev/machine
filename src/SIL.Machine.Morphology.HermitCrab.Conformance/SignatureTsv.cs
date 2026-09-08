using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>One well-formed 5-column row of the batch TSV format (see conformance/PROTOCOL.md).</summary>
public readonly struct TsvRow(int idx, string word, long ms, string status, string signature)
{
    public readonly int Idx = idx;
    public readonly string Word = word;
    public readonly long Ms = ms;
    public readonly string Status = status;
    public readonly string Signature = signature;
}

/// <summary>
/// Parsing and comparison for the batch TSV format, per conformance/PROTOCOL.md sections 2-4:
/// only word/status/signature are compared; idx and ms are ignored; any line that isn't a
/// well-formed 5-column row (in particular the "STARTED" 3-column sentinel) is skipped; a
/// signature's ";"-separated entries are compared as a multiset, not a plain string.
/// </summary>
public static class SignatureTsv
{
    /// <summary>Reads a batch TSV file, silently skipping malformed/sentinel lines.</summary>
    public static List<TsvRow> ReadFile(string path)
    {
        var rows = new List<TsvRow>();
        foreach (string line in File.ReadAllLines(path))
        {
            if (TryParseRow(line, out TsvRow row))
                rows.Add(row);
        }
        return rows;
    }

    public static bool TryParseRow(string line, out TsvRow row)
    {
        row = default;
        if (string.IsNullOrEmpty(line))
            return false;
        string[] cols = line.Split('\t');
        // A well-formed row has exactly 5 columns (idx, word, ms, status, signature). The
        // 3-column "STARTED" sentinel and any other malformed line is not comparison data.
        if (cols.Length != 5)
            return false;
        if (!int.TryParse(cols[0], out int idx))
            return false;
        if (!long.TryParse(cols[2], out long ms))
            return false;
        // A well-formed row's status is always "ok" or "SKIPPED" (see PROTOCOL.md section 2); any
        // other value means the line isn't actually a comparison-worthy result row.
        if (cols[3] != "ok" && cols[3] != "SKIPPED")
            return false;
        row = new TsvRow(idx, cols[1], ms, cols[3], cols[4]);
        return true;
    }

    /// <summary>The ";"-separated signature entries as a multiset (sorted list, for equality/diff display).</summary>
    public static List<string> SplitSignature(string signature)
    {
        if (signature == "-")
            return new List<string>();
        return signature.Split(';').OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
