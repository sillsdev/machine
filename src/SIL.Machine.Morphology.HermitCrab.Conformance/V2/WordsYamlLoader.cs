using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.V2;

/// <summary>
/// Strict parser for a v2 fixture's <c>words.yaml</c>, per
/// docs/conformance-language-suite-plan.md section 2.1: a fixed key vocabulary (unknown keys are
/// hard errors) and "plain YAML 1.2 subset only: no anchors, aliases, merge keys, or custom tags".
/// </summary>
public static class WordsYamlLoader
{
    // The complete, fixed key vocabulary at each of the three mapping levels. Anything else
    // encountered is a hard parse error -- this is the mechanism behind "unknown keys are errors".
    private static readonly HashSet<string> FrontMatterKeys = new(StringComparer.Ordinal)
    {
        "language",
        "inspired_by",
        "sources",
        "requires",
        "budget_ms",
        "expect_crash",
        "words",
    };
    private static readonly HashSet<string> WordKeys = new(StringComparer.Ordinal)
    {
        "word",
        "note",
        "provenance",
        "expect_fail",
        "expect_skip",
        "blocked_by",
        "exercises",
        "parses",
    };
    private static readonly HashSet<string> ParseKeys = new(StringComparer.Ordinal)
    {
        "signature",
        "gloss",
        "guess",
        "rules",
        "exercises",
    };

    public static WordsYaml Load(string path)
    {
        string text = File.ReadAllText(path);
        try
        {
            RejectNonPlainConstructs(text, path);

            var yamlStream = new YamlStream();
            using (var reader = new StringReader(text))
                yamlStream.Load(reader);

            if (yamlStream.Documents.Count != 1)
                throw new WordsYamlException(path, "expected exactly one YAML document");

            if (yamlStream.Documents[0].RootNode is not YamlMappingNode root)
                throw new WordsYamlException(path, "top-level YAML node must be a mapping");

            return ParseFrontMatter(root, path);
        }
        catch (YamlException ex)
        {
            throw new WordsYamlException(path, $"YAML syntax error at {ex.Start}-{ex.End}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Scans the raw parser event stream (not the composed node tree -- by the time
    /// <see cref="YamlStream"/> composes a tree, an alias has already been resolved into a shared
    /// node reference and is no longer visible as such) for anchors, aliases, and non-core tags,
    /// per the plan's "no anchors, aliases, merge keys, or custom tags" rule. Merge keys ("&lt;&lt;")
    /// are a mapping-key convention layered on plain scalars, not a distinct event type, so those
    /// are rejected in <see cref="RequireMapping"/> instead, where key strings are actually read.
    /// </summary>
    private static void RejectNonPlainConstructs(string text, string path)
    {
        var parser = new Parser(new StringReader(text));
        while (parser.MoveNext())
        {
            ParsingEvent evt = parser.Current;
            if (evt is AnchorAlias)
                throw new WordsYamlException(path, "YAML aliases (*name) are not allowed in words.yaml");

            if (evt is NodeEvent nodeEvent)
            {
                if (!nodeEvent.Anchor.IsEmpty)
                    throw new WordsYamlException(path, "YAML anchors (&name) are not allowed in words.yaml");
                if (
                    !nodeEvent.Tag.IsEmpty
                    && !nodeEvent.Tag.Value.StartsWith("tag:yaml.org,2002:", StringComparison.Ordinal)
                )
                {
                    throw new WordsYamlException(
                        path,
                        $"custom YAML tag '{nodeEvent.Tag.Value}' is not allowed in words.yaml"
                    );
                }
            }
        }
    }

    private static WordsYaml ParseFrontMatter(YamlMappingNode root, string path)
    {
        var doc = new WordsYaml();
        Dictionary<string, YamlNode> map = RequireMapping(root, FrontMatterKeys, path, "front matter");

        doc.Language = RequireScalar(map, "language", path, "front matter");
        AppendScalarList(map, "inspired_by", doc.InspiredBy, path, "front matter");
        AppendScalarList(map, "sources", doc.Sources, path, "front matter");
        AppendScalarList(map, "requires", doc.Requires, path, "front matter");
        if (map.TryGetValue("budget_ms", out YamlNode budgetNode))
            doc.BudgetMs = long.Parse(RequireScalarNode(budgetNode, path, "front matter.budget_ms"));
        if (map.TryGetValue("expect_crash", out YamlNode crashNode))
            doc.ExpectCrash = ParseBool(RequireScalarNode(crashNode, path, "front matter.expect_crash"), path);

        if (!map.TryGetValue("words", out YamlNode wordsNode) || wordsNode is not YamlSequenceNode wordsSeq)
            throw new WordsYamlException(path, "front matter must have a 'words' sequence");
        if (wordsSeq.Children.Count == 0)
            throw new WordsYamlException(path, "'words' must contain at least one word entry");

        foreach (YamlNode wordNode in wordsSeq.Children)
            doc.Words.Add(ParseWord(wordNode, path));

        return doc;
    }

    private static WordEntry ParseWord(YamlNode node, string path)
    {
        if (node is not YamlMappingNode mapNode)
            throw new WordsYamlException(path, "each 'words' entry must be a mapping");
        Dictionary<string, YamlNode> map = RequireMapping(mapNode, WordKeys, path, "word");

        var entry = new WordEntry
        {
            Word = RequireScalar(map, "word", path, "word"),
            Note = RequireScalar(map, "note", path, "word"),
        };
        if (map.TryGetValue("provenance", out YamlNode provNode))
            entry.Provenance = RequireScalarNode(provNode, path, $"word '{entry.Word}'.provenance");
        if (map.TryGetValue("expect_fail", out YamlNode failNode))
            entry.ExpectFail = ParseBool(RequireScalarNode(failNode, path, $"word '{entry.Word}'.expect_fail"), path);
        if (map.TryGetValue("expect_skip", out YamlNode skipNode))
            entry.ExpectSkip = ParseBool(RequireScalarNode(skipNode, path, $"word '{entry.Word}'.expect_skip"), path);
        AppendScalarList(map, "blocked_by", entry.BlockedBy, path, $"word '{entry.Word}'");
        AppendScalarList(map, "exercises", entry.Exercises, path, $"word '{entry.Word}'");

        if (entry.ExpectFail && entry.ExpectSkip)
        {
            throw new WordsYamlException(
                path,
                $"word '{entry.Word}': 'expect_fail' and 'expect_skip' are mutually exclusive "
                    + "(a word is either a genuine zero-parse 'ok' or a SKIPPED InvalidShapeException, not both)"
            );
        }
        if (entry.BlockedBy.Count > 0 && !entry.ExpectFail)
        {
            throw new WordsYamlException(
                path,
                $"word '{entry.Word}': 'blocked_by' is only meaningful on an 'expect_fail' word"
            );
        }

        bool hasParses = map.TryGetValue("parses", out YamlNode parsesNode);
        if (entry.ExpectFail || entry.ExpectSkip)
        {
            if (hasParses)
            {
                throw new WordsYamlException(
                    path,
                    $"word '{entry.Word}': expect_fail/expect_skip words must not have 'parses'"
                );
            }
        }
        else
        {
            if (!hasParses || parsesNode is not YamlSequenceNode parsesSeq || parsesSeq.Children.Count == 0)
            {
                throw new WordsYamlException(
                    path,
                    $"word '{entry.Word}': non-expect_fail words must have at least one entry under 'parses'"
                );
            }
            foreach (YamlNode parseNode in parsesSeq.Children)
                entry.Parses.Add(ParseParse(parseNode, entry.Word, path));
        }

        return entry;
    }

    private static ParseEntry ParseParse(YamlNode node, string word, string path)
    {
        if (node is not YamlMappingNode mapNode)
            throw new WordsYamlException(path, $"word '{word}': each 'parses' entry must be a mapping");
        Dictionary<string, YamlNode> map = RequireMapping(mapNode, ParseKeys, path, $"word '{word}'.parses[]");

        var parse = new ParseEntry { Signature = RequireScalar(map, "signature", path, $"word '{word}'.parses[]") };
        if (map.TryGetValue("gloss", out YamlNode glossNode))
            parse.Gloss = RequireScalarNode(glossNode, path, $"word '{word}'.parses[].gloss");
        if (map.TryGetValue("guess", out YamlNode guessNode))
            parse.Guess = ParseBool(RequireScalarNode(guessNode, path, $"word '{word}'.parses[].guess"), path);
        if (!map.ContainsKey("rules"))
            throw new WordsYamlException(path, $"word '{word}': every parse requires a 'rules' list (may be empty [])");
        AppendScalarList(map, "rules", parse.Rules, path, $"word '{word}'.parses[]");
        AppendScalarList(map, "exercises", parse.Exercises, path, $"word '{word}'.parses[]");

        return parse;
    }

    /// <summary>
    /// Reads a mapping node's keys into a dictionary, hard-failing on any key not in
    /// <paramref name="allowedKeys"/> (the "unknown keys are hard errors" rule) and on any
    /// non-scalar key (which would indicate a merge key or other non-plain construct).
    /// </summary>
    private static Dictionary<string, YamlNode> RequireMapping(
        YamlMappingNode node,
        HashSet<string> allowedKeys,
        string path,
        string context
    )
    {
        var map = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
        foreach (KeyValuePair<YamlNode, YamlNode> entry in node.Children)
        {
            if (entry.Key is not YamlScalarNode keyScalar || keyScalar.Value == null)
                throw new WordsYamlException(path, $"{context}: mapping keys must be plain scalars");
            string key = keyScalar.Value;
            if (key == "<<")
                throw new WordsYamlException(path, $"{context}: YAML merge keys ('<<') are not allowed");
            if (!allowedKeys.Contains(key))
            {
                throw new WordsYamlException(
                    path,
                    $"{context}: unknown key '{key}' (allowed: {string.Join(", ", allowedKeys.OrderBy(k => k))})"
                );
            }
            if (map.ContainsKey(key))
                throw new WordsYamlException(path, $"{context}: duplicate key '{key}'");
            map[key] = entry.Value;
        }
        return map;
    }

    private static string RequireScalar(Dictionary<string, YamlNode> map, string key, string path, string context)
    {
        if (!map.TryGetValue(key, out YamlNode node))
            throw new WordsYamlException(path, $"{context}: missing required key '{key}'");
        return RequireScalarNode(node, path, $"{context}.{key}");
    }

    private static string RequireScalarNode(YamlNode node, string path, string context)
    {
        if (node is not YamlScalarNode scalar || scalar.Value == null || scalar.Value.Length == 0)
            throw new WordsYamlException(path, $"{context}: expected a non-empty scalar value");
        return scalar.Value;
    }

    private static void AppendScalarList(
        Dictionary<string, YamlNode> map,
        string key,
        List<string> target,
        string path,
        string context
    )
    {
        if (!map.TryGetValue(key, out YamlNode node))
            return;
        if (node is not YamlSequenceNode seq)
            throw new WordsYamlException(path, $"{context}.{key}: expected a sequence");
        foreach (YamlNode child in seq.Children)
            target.Add(RequireScalarNode(child, path, $"{context}.{key}[]"));
    }

    private static bool ParseBool(string value, string path)
    {
        return value switch
        {
            "true" => true,
            "false" => false,
            _ => throw new WordsYamlException(path, $"expected 'true' or 'false', got '{value}'"),
        };
    }
}

public class WordsYamlException(string path, string message, Exception inner = null)
    : Exception($"{path}: {message}", inner) { }
