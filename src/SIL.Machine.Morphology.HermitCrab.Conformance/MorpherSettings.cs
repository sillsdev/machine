using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// A fixture's <c>morpher:</c> front-matter block: <see cref="Morpher"/> host settings the run's
/// ground truth was generated under, keyed by the FieldWorks <c>&lt;ParserParameters&gt;&lt;HC&gt;</c>
/// element names that set them (<c>HCParser.LoadParser</c>). Every property is null when the block
/// omits it, which means the <see cref="Morpher"/> constructor default. Only settings whose effect
/// is a parse result are accepted; anything else is a
/// <see cref="WordsYamlLoader"/> error.
/// </summary>
public class MorpherSettings
{
    /// <summary>The <c>requires</c> capability a fixture with a non-empty block must declare.</summary>
    public const string Capability = "morpher-settings";

    /// <summary>
    /// No overrides: every <see cref="Morpher"/> property at its constructor default. Named so a caller
    /// that deliberately ignores a fixture's block says so.
    /// </summary>
    public static MorpherSettings Defaults => new();

    /// <summary><c>DelReapps</c>: <see cref="Morpher.DeletionReapplications"/>, at least 0.</summary>
    public int? DelReapps { get; set; }

    /// <summary><c>MaxRoots</c>: <see cref="Morpher.MaxStemCount"/>, at least 1.</summary>
    public int? MaxRoots { get; set; }

    /// <summary><c>MergeAnalyses</c>: <see cref="Morpher.MergeEquivalentAnalyses"/>.</summary>
    public bool? MergeAnalyses { get; set; }

    /// <summary><c>MakePartialsFinal</c>: <see cref="Morpher.AlwaysEnforceFinalTemplates"/>.</summary>
    public bool? MakePartialsFinal { get; set; }

    /// <summary>Per-call ParseWord guessRoot flag; this is not a Morpher property.</summary>
    public bool? GuessRoots { get; set; }

    public bool IsEmpty =>
        DelReapps == null
        && MaxRoots == null
        && MergeAnalyses == null
        && MakePartialsFinal == null
        && GuessRoots == null;

    /// <summary>
    /// Resolve the per-call root-guess flag for a word, preserving legacy per-word behavior unless
    /// this run explicitly supplies <c>GuessRoots</c>.
    /// </summary>
    public bool GuessRootsFor(WordEntry word)
    {
        return GuessRoots ?? word.Parses.Any(parse => parse.Guess);
    }

    /// <summary>Sets every declared value on <paramref name="morpher"/>, leaving the rest at their defaults.</summary>
    public void ApplyTo(Morpher morpher)
    {
        if (DelReapps is int delReapps)
            morpher.DeletionReapplications = delReapps;
        if (MaxRoots is int maxRoots)
            morpher.MaxStemCount = maxRoots;
        if (MergeAnalyses is bool mergeAnalyses)
            morpher.MergeEquivalentAnalyses = mergeAnalyses;
        if (MakePartialsFinal is bool makePartialsFinal)
            morpher.AlwaysEnforceFinalTemplates = makePartialsFinal;
    }

    /// <summary>The declared values by FieldWorks element name, for manifest.json and adapters.</summary>
    public SortedDictionary<string, object> ToDictionary()
    {
        var values = new SortedDictionary<string, object>(System.StringComparer.Ordinal);
        if (DelReapps is int delReapps)
            values["DelReapps"] = delReapps;
        if (MaxRoots is int maxRoots)
            values["MaxRoots"] = maxRoots;
        if (MergeAnalyses is bool mergeAnalyses)
            values["MergeAnalyses"] = mergeAnalyses;
        if (MakePartialsFinal is bool makePartialsFinal)
            values["MakePartialsFinal"] = makePartialsFinal;
        if (GuessRoots is bool guessRoots)
            values["GuessRoots"] = guessRoots;
        return values;
    }
}
