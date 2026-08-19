#nullable enable
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

public static class SemanticCoverageInventory
{
    public static SemanticInventory Generate(SemanticCoverageSourceSet sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentException.ThrowIfNullOrEmpty(sources.DtdPath);
        ArgumentNullException.ThrowIfNull(sources.DtdText);
        ArgumentNullException.ThrowIfNull(sources.CSharpSources);
        if (sources.CSharpSources.Count == 0)
        {
            SemanticInventory dtdOnly = DtdInventoryReader.Read(sources.DtdPath, sources.DtdText);
            return string.IsNullOrEmpty(sources.ToolchainFingerprint)
                ? dtdOnly
                : dtdOnly with { SourceHash = CompositeHash(dtdOnly.SourceHash, "", sources.ToolchainFingerprint) };
        }

        var duplicatePaths = sources.CSharpSources
            .GroupBy(source => NormalizePath(source.RelativePath), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (duplicatePaths.Length != 0)
        {
            throw new ArgumentException(
                $"Duplicate C# source paths: {string.Join(", ", duplicatePaths)}",
                nameof(sources)
            );
        }

        SemanticInventory csharp = CSharpInventoryReader.Read(
            sources.CSharpSources,
            sources.CompleteProjects ?? Array.Empty<string>());
        return Compose(sources.DtdPath, sources.DtdText, csharp, sources.ToolchainFingerprint);
    }

    internal static SemanticInventory Compose(
        string dtdPath,
        string dtdText,
        SemanticInventory csharp,
        string toolchainFingerprint)
    {
        SemanticInventory dtd = DtdInventoryReader.Read(dtdPath, dtdText);
        return new SemanticInventory(
            dtd.Profile,
            CompositeHash(dtd.SourceHash, csharp.SourceHash, toolchainFingerprint),
            InventorySurfaceFactory.Sort(dtd.Surfaces.Concat(csharp.Surfaces)),
            dtd.Diagnostics.Concat(csharp.Diagnostics)
                .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.SubjectId, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Location, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                .ToArray()
        );
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string CompositeHash(string dtdHash, string csharpHash, string toolchainFingerprint = "")
    {
        string material = $"dtd\n{dtdHash}\ncsharp\n{csharpHash}\n";
        if (!string.IsNullOrEmpty(toolchainFingerprint))
            material += $"toolchain\n{toolchainFingerprint}\n";
        byte[] input = Encoding.UTF8.GetBytes(material);
        return Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
    }
}
