#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed record GraphHashes(string GraphInputHash, string ToolchainHash, string GraphHash);

internal static class CompilationGraphHashing
{
    internal static GraphHashes Compute(CompilationGraphHashInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        byte[] graphInput = CanonicalJson.SerializeUtf8(ToGraphInputDocument(inputs));
        byte[] toolchain = CanonicalJson.SerializeUtf8(ToToolchainDocument(inputs.Toolchain));
        byte[] graph = CanonicalJson.SerializeUtf8(ToGraphDocument(inputs));
        return new GraphHashes(Hash(graphInput), Hash(toolchain), Hash(graph));
    }

    private static object ToGraphInputDocument(CompilationGraphHashInputs inputs) => new
    {
        schemaVersion = inputs.SchemaVersion,
        projects = inputs.Projects.OrderBy(project => project.Id, StringComparer.Ordinal).Select(project => new
        {
            id = project.Id,
            path = project.LogicalPath,
            targetFramework = project.TargetFramework,
        }),
        profiles = inputs.Profiles.OrderBy(profile => profile.Id, StringComparer.Ordinal).Select(profile => new
        {
            id = profile.Id,
            symbols = profile.Symbols.OrderBy(symbol => symbol, StringComparer.Ordinal),
        }),
        nodes = inputs.Nodes.OrderBy(node => NodeIdentity(node.Key), StringComparer.Ordinal).Select(ToNodeDocument),
        edges = inputs.Edges.OrderBy(edge => edge.FromProjectId, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToProjectId, StringComparer.Ordinal)
            .Select(edge => new { from = edge.FromProjectId, to = edge.ToProjectId }),
        captureTarget = ToFileDocument(inputs.CaptureTarget),
        lockFile = new
        {
            present = inputs.LockFile.IsPresent,
            file = inputs.LockFile.File is null ? null : ToFileDocument(inputs.LockFile.File),
        },
    };

    private static object ToToolchainDocument(ToolchainHashInput toolchain) => new
    {
        sdkVersion = toolchain.SdkVersion,
        msBuildVersion = toolchain.MSBuildVersion,
        roslynIdentity = toolchain.RoslynIdentity,
        compilerIdentity = toolchain.CompilerIdentity,
        loaderIdentity = toolchain.LoaderIdentity,
        files = toolchain.Files.OrderBy(file => file.LogicalPath, StringComparer.Ordinal).Select(ToFileDocument),
    };

    private static object ToGraphDocument(CompilationGraphHashInputs inputs) => new
    {
        schemaVersion = inputs.SchemaVersion,
        profiles = inputs.Profiles.OrderBy(profile => profile.Id, StringComparer.Ordinal).Select(profile => new
        {
            id = profile.Id,
            symbols = profile.Symbols.OrderBy(symbol => symbol, StringComparer.Ordinal),
        }),
        nodes = inputs.Nodes.OrderBy(node => NodeIdentity(node.Key), StringComparer.Ordinal).Select(node => new
        {
            key = NodeIdentity(node.Key),
            fingerprint = Hash(CanonicalJson.SerializeUtf8(ToNodeDocument(node))),
        }),
        edges = inputs.Edges.OrderBy(edge => edge.FromProjectId, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToProjectId, StringComparer.Ordinal)
            .Select(edge => new { from = edge.FromProjectId, to = edge.ToProjectId }),
    };

    private static object ToNodeDocument(NodeHashInput node) => new
    {
        key = NodeIdentity(node.Key),
        settings = node.Settings,
        arguments = node.Arguments.OrderBy(argument => argument.Ordinal).Select(argument => new
        {
            ordinal = argument.Ordinal,
            value = argument.Value,
        }),
        sources = node.Sources.OrderBy(source => source.Ordinal).Select(source => new
        {
            ordinal = source.Ordinal,
            file = ToFileDocument(source.File),
        }),
        references = node.References.OrderBy(reference => reference.Identity, StringComparer.Ordinal).Select(reference => new
        {
            identity = reference.Identity,
            file = ToFileDocument(reference.File),
            aliases = reference.Aliases.OrderBy(alias => alias, StringComparer.Ordinal),
            embedInteropTypes = reference.EmbedInteropTypes,
        }),
        projectReferences = node.ProjectReferences.OrderBy(reference => reference.ProjectId, StringComparer.Ordinal).Select(reference => new
        {
            projectId = reference.ProjectId,
            metadata = reference.Metadata,
        }),
        analyzers = node.Analyzers.OrderBy(analyzer => analyzer.Identity, StringComparer.Ordinal).Select(analyzer => new
        {
            identity = analyzer.Identity,
            file = ToFileDocument(analyzer.File),
        }),
        additionalFiles = node.AdditionalFiles.OrderBy(file => file.LogicalPath, StringComparer.Ordinal).Select(ToFileDocument),
        editorConfigFiles = node.EditorConfigFiles.OrderBy(file => file.LogicalPath, StringComparer.Ordinal).Select(ToFileDocument),
        usings = node.Usings.OrderBy(file => file.LogicalPath, StringComparer.Ordinal).Select(ToFileDocument),
        assets = node.Assets.OrderBy(file => file.LogicalPath, StringComparer.Ordinal).Select(ToFileDocument),
        imports = node.Imports.OrderBy(file => file.LogicalPath, StringComparer.Ordinal).Select(ToFileDocument),
    };

    private static object ToFileDocument(GraphHashFile file) => new
    {
        path = file.LogicalPath,
        kind = file.Kind.ToString(),
        content = Convert.ToBase64String(NormalizeContent(file)),
    };

    private static byte[] NormalizeContent(GraphHashFile file)
    {
        if (file.Kind == GraphHashFileKind.Binary)
            return file.Content.ToArray();

        if (file.Kind == GraphHashFileKind.Json)
            return CanonicalJson.NormalizeJson(file.Content.AsSpan());

        string text;
        try
        {
            text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(file.Content.AsSpan());
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException($"Text file '{file.LogicalPath}' is not valid UTF-8.", exception);
        }
        return Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n'));
    }

    private static string NodeIdentity(RepositoryGraphNodeKey key) =>
        $"{key.ProjectId}/{key.TargetFramework}/{key.ProfileId}";

    private static string Hash(ReadOnlySpan<byte> bytes)
    {
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(bytes, digest);
        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
