#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>Builds the <see cref="ConformanceManifest"/> from the fixtures under a suite root.</summary>
/// <remarks>Nothing here interprets a grammar or restates a words file; each contributes a
/// suite-relative path and a hash. The authored files stay the only ground truth.</remarks>
public static class ConformanceManifestGenerator
{
    /// <summary>The suite's manifest, relative to the suite root. The suite's generator writes it.</summary>
    public const string ManifestRelativePath = "generated/hc-conformance-manifest.v2.json";

    /// <summary>The DTD as the manifest names it, relative to the suite root.</summary>
    public const string DtdRelativePath = "HermitCrabInput.dtd";

    /// <summary>Files the manifest's own validity depends on, beyond the fixtures themselves, relative to
    /// the suite root.</summary>
    private static readonly string[] AdditionalSourceFiles =
    {
        "constructs.txt",
        "parity-check.py",
        "schema/words.schema.json",
        "schema/conformance-manifest.schema.json",
    };

    public static ConformanceManifest Generate(string suiteRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(suiteRoot));
        string dtdRelative = DtdRelativePath;
        string dtdHash = HashFile(Path.Combine(root, dtdRelative.Replace('/', Path.DirectorySeparatorChar)));

        List<Fixture> fixtures = Fixture.DiscoverAll(root);
        var mapped = fixtures
            .Select(fixture => MapFixture(fixture, root))
            .OrderBy(fixture => fixture.FixtureId, StringComparer.Ordinal)
            .ToArray();

        return new ConformanceManifest(
            ConformanceManifest.ManifestFormat,
            ConformanceManifest.WordsFormat,
            ConformanceManifest.GrammarFormat,
            dtdRelative,
            dtdHash,
            SourceHash(root, mapped, dtdRelative, dtdHash),
            mapped
        );
    }

    private static ManifestFixture MapFixture(Fixture fixture, string root)
    {
        string fixtureId = RelativePath(fixture.Directory, root);
        string grammarPath = fixtureId + "/grammar.xml";
        string wordsPath = fixtureId + "/words.yaml";
        WordsYaml words = fixture.Words;

        string[] duplicates = words
            .Words.Select(word => word.Word)
            .GroupBy(input => input, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(input => input, StringComparer.Ordinal)
            .ToArray();
        if (duplicates.Length != 0)
        {
            throw new InvalidDataException(
                $"Fixture '{fixtureId}' repeats input(s): {string.Join(", ", duplicates)}. Inputs must be unique within a fixture."
            );
        }

        if (words.ExpectCrash && words.Words.Count != 1)
        {
            throw new InvalidDataException(
                $"Fixture '{fixtureId}' declares expect_crash and must contain exactly one case, but has {words.Words.Count}."
            );
        }

        return new ManifestFixture(
            fixtureId,
            fixtureId.StartsWith("languages/", StringComparison.Ordinal) ? "language" : "edge-case",
            words.Language,
            grammarPath,
            HashFile(Path.Combine(root, grammarPath.Replace('/', Path.DirectorySeparatorChar))),
            wordsPath,
            HashFile(Path.Combine(root, wordsPath.Replace('/', Path.DirectorySeparatorChar))),
            words.Words.Count,
            words.ExpectCrash
        );
    }

    private static string RelativePath(string path, string basePath) =>
        Path.GetRelativePath(basePath, path).Replace('\\', '/');

    /// <summary>Hash of every file the manifest's validity rests on.</summary>
    /// <remarks>Path- and EOL-invariant: only suite-relative paths and LF-normalized text
    /// participate, so relocating the checkout or changing line endings cannot move it.</remarks>
    private static string SourceHash(
        string root,
        IReadOnlyList<ManifestFixture> fixtures,
        string dtdRelative,
        string dtdHash
    )
    {
        var text = new StringBuilder();
        text.Append(ConformanceManifest.ManifestFormat).Append('\n');
        text.Append(dtdRelative).Append('\0').Append(dtdHash).Append('\n');
        foreach (ManifestFixture fixture in fixtures)
        {
            text.Append(fixture.FixtureId)
                .Append('\0')
                .Append(fixture.GrammarSha256)
                .Append('\0')
                .Append(fixture.WordsSha256)
                .Append('\n');
        }

        foreach (string relative in AdditionalSourceFiles.OrderBy(path => path, StringComparer.Ordinal))
        {
            string absolute = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            text.Append(relative)
                .Append('\0')
                .Append(File.Exists(absolute) ? HashFile(absolute) : "<absent>")
                .Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()))).ToLowerInvariant();
    }

    /// <summary>SHA-256 of a file's text with line endings normalized to LF.</summary>
    internal static string HashFile(string path)
    {
        string content = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
    }
}
