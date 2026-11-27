using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// One row of the suite's <c>status.yaml</c>: a tracked departure from the reference (ADR 0007). A row without a
/// <see cref="Word"/> concerns the whole test case, and its <see cref="Upstream"/> entry is what the runner and the
/// FieldWorks-producible gate resolve. A row with a word records the intended answer the reference does not yet give.
/// </summary>
public sealed record StatusException(
    string Case,
    string Word,
    string Kind,
    UpstreamEntry Upstream,
    string Reason,
    string Intended
)
{
    public const string ReferenceDefect = "reference-defect";
    public const string PendingUpstream = "pending-upstream";
}

/// <summary>Strict reader for the suite's <c>status.yaml</c>; unknown keys are hard errors, like <c>words.yaml</c>.</summary>
public static class StatusYamlLoader
{
    public const string FileName = "status.yaml";

    private static readonly HashSet<string> TopKeys = new(StringComparer.Ordinal) { "version", "exceptions" };
    private static readonly HashSet<string> RowKeys = new(StringComparer.Ordinal)
    {
        "case",
        "word",
        "kind",
        "ref",
        "ref_type",
        "head",
        "affects",
        "reason",
        "intended",
    };

    /// <summary>The rows of <c>status.yaml</c> under <paramref name="suiteRoot"/>; empty when the file does not exist.</summary>
    public static List<StatusException> LoadIfPresent(string suiteRoot)
    {
        string path = Path.Combine(suiteRoot, FileName);
        return File.Exists(path) ? Load(path) : new List<StatusException>();
    }

    public static List<StatusException> Load(string path)
    {
        try
        {
            var stream = new YamlStream();
            using (var reader = new StringReader(File.ReadAllText(path)))
                stream.Load(reader);
            if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode root)
                throw new WordsYamlException(path, "expected one YAML document holding a mapping");

            Dictionary<string, YamlNode> top = Keys(root, TopKeys, path, "status.yaml");
            if (Scalar(top, "version", path, "status.yaml") != "1")
                throw new WordsYamlException(path, "status.yaml: version must be 1");
            if (!top.TryGetValue("exceptions", out YamlNode rowsNode) || rowsNode is not YamlSequenceNode rows)
                throw new WordsYamlException(path, "status.yaml: 'exceptions' must be a sequence");

            var result = new List<StatusException>();
            var seen = new HashSet<(string, string)>();
            foreach (YamlNode rowNode in rows.Children)
            {
                if (rowNode is not YamlMappingNode rowMap)
                    throw new WordsYamlException(path, "each exception must be a mapping");
                StatusException row = ParseRow(Keys(rowMap, RowKeys, path, "exceptions[]"), path);
                if (!seen.Add((row.Case, row.Word + "\0" + row.Upstream.Ref)))
                    throw new WordsYamlException(
                        path,
                        $"duplicate exception for {row.Case} {row.Word} {row.Upstream.Ref}"
                    );
                result.Add(row);
            }
            return result;
        }
        catch (YamlException ex)
        {
            throw new WordsYamlException(path, $"YAML syntax error at {ex.Start}-{ex.End}: {ex.Message}", ex);
        }
    }

    private static StatusException ParseRow(Dictionary<string, YamlNode> row, string path)
    {
        const string Context = "exceptions[]";
        string testCase = Scalar(row, "case", path, Context);
        string word = row.ContainsKey("word") ? Scalar(row, "word", path, Context) : "";
        string kind = Scalar(row, "kind", path, Context);
        string reference = Scalar(row, "ref", path, Context);
        string refType = Scalar(row, "ref_type", path, Context);
        string affects = Scalar(row, "affects", path, Context);
        string head = row.ContainsKey("head") ? Scalar(row, "head", path, Context) : "";
        string reason = Scalar(row, "reason", path, Context);
        string intended = row.ContainsKey("intended") ? Scalar(row, "intended", path, Context) : "";

        if (kind is not (StatusException.ReferenceDefect or StatusException.PendingUpstream))
            throw new WordsYamlException(
                path,
                $"{testCase}: kind must be 'reference-defect' or 'pending-upstream', got '{kind}'"
            );
        if (!UpstreamResolver.TryParseRef(reference, out string repo, out _))
            throw new WordsYamlException(
                path,
                $"{testCase}: ref '{reference}' is not owner/repo#number or jira:KEY-123"
            );
        if (refType is not ("pr" or "issue"))
            throw new WordsYamlException(path, $"{testCase}: ref_type must be 'pr' or 'issue', got '{refType}'");
        if (affects is not ("engine" or "loader"))
            throw new WordsYamlException(path, $"{testCase}: affects must be 'engine' or 'loader', got '{affects}'");
        bool fullHead = head.Length == 40 && head.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
        if (refType == "pr" && !fullHead)
            throw new WordsYamlException(path, $"{testCase}: a pr needs a full 40-hex 'head'");
        if (refType == "issue" && head.Length != 0)
            throw new WordsYamlException(path, $"{testCase}: only a pr has a 'head'");
        if (repo == "jira" && refType != "issue")
            throw new WordsYamlException(path, $"{testCase}: a Jira key is an issue");
        if (word.Length != 0 && intended.Length == 0)
            throw new WordsYamlException(
                path,
                $"{testCase} {word}: a word exception records the intended answer in 'intended'"
            );
        if (word.Length == 0 && intended.Length != 0)
            throw new WordsYamlException(path, $"{testCase}: 'intended' belongs to a word exception");

        return new StatusException(
            testCase,
            word,
            kind,
            new UpstreamEntry(reference, refType, head, affects, reason),
            reason,
            intended
        );
    }

    private static Dictionary<string, YamlNode> Keys(
        YamlMappingNode node,
        HashSet<string> allowed,
        string path,
        string context
    )
    {
        var map = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
        foreach (KeyValuePair<YamlNode, YamlNode> entry in node.Children)
        {
            if (entry.Key is not YamlScalarNode key || key.Value == null)
                throw new WordsYamlException(path, $"{context}: mapping keys must be plain scalars");
            if (!allowed.Contains(key.Value))
                throw new WordsYamlException(
                    path,
                    $"{context}: unknown key '{key.Value}' (allowed: {string.Join(", ", allowed.OrderBy(k => k, StringComparer.Ordinal))})"
                );
            if (!map.TryAdd(key.Value, entry.Value))
                throw new WordsYamlException(path, $"{context}: duplicate key '{key.Value}'");
        }
        return map;
    }

    private static string Scalar(Dictionary<string, YamlNode> map, string key, string path, string context)
    {
        if (!map.TryGetValue(key, out YamlNode node))
            throw new WordsYamlException(path, $"{context}: missing required key '{key}'");
        if (node is not YamlScalarNode scalar || string.IsNullOrEmpty(scalar.Value))
            throw new WordsYamlException(path, $"{context}.{key}: expected a non-empty scalar value");
        return scalar.Value;
    }
}
