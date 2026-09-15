#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed record AnalyzerMetadataInspection(
    string Path,
    bool IsSourceGenerator,
    bool LoadedAssembly,
    string AssemblyIdentity,
    string Sha256,
    AnalyzerDisposition Disposition,
    string? ReferencePackVersion
);

internal static class AnalyzerMetadataInspector
{
    internal static AnalyzerMetadataInspection Inspect(
        string path,
        IReadOnlyCollection<string>? admittedSdkAnalyzerDirectories = null
    )
    {
        try
        {
            string fullPath = Path.GetFullPath(path);
            string assemblyIdentity =
                AssemblyName.GetAssemblyName(fullPath).FullName
                ?? throw new BadImageFormatException("Analyzer has no assembly identity.");
            string sha256;
            using (FileStream hashStream = File.OpenRead(fullPath))
                sha256 = Convert.ToHexString(SHA256.HashData(hashStream)).ToLowerInvariant();

            using FileStream stream = File.OpenRead(fullPath);
            using var pe = new PEReader(stream);
            if (!pe.HasMetadata)
                throw new BadImageFormatException("Analyzer has no CLI metadata.");
            MetadataReader metadata = pe.GetMetadataReader();
            var interfaces = new HashSet<string>(StringComparer.Ordinal);
            foreach (TypeDefinitionHandle handle in metadata.TypeDefinitions)
            {
                TypeDefinition type = metadata.GetTypeDefinition(handle);
                foreach (InterfaceImplementationHandle implementationHandle in type.GetInterfaceImplementations())
                {
                    string? interfaceName = TryGetTypeName(
                        metadata,
                        metadata.GetInterfaceImplementation(implementationHandle).Interface
                    );
                    if (interfaceName is not null)
                        interfaces.Add(interfaceName);
                }

                foreach (CustomAttributeHandle attributeHandle in type.GetCustomAttributes())
                {
                    string? attributeName = TryGetAttributeTypeName(
                        metadata,
                        metadata.GetCustomAttribute(attributeHandle)
                    );
                    if (attributeName == "Microsoft.CodeAnalysis.GeneratorAttribute")
                    {
                        return CreateInspection(
                            fullPath,
                            true,
                            assemblyIdentity,
                            sha256,
                            admittedSdkAnalyzerDirectories
                        );
                    }
                }
            }

            bool generator =
                interfaces.Contains("Microsoft.CodeAnalysis.ISourceGenerator")
                || interfaces.Contains("Microsoft.CodeAnalysis.IIncrementalGenerator");
            return CreateInspection(fullPath, generator, assemblyIdentity, sha256, admittedSdkAnalyzerDirectories);
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or BadImageFormatException
                        or InvalidOperationException
            )
        {
            throw new CompilerInputException(
                "analyzer-metadata-diagnostic",
                $"Cannot inspect analyzer metadata '{path}'.",
                exception
            );
        }
    }

    private static AnalyzerMetadataInspection CreateInspection(
        string path,
        bool isSourceGenerator,
        string assemblyIdentity,
        string sha256,
        IReadOnlyCollection<string>? admittedSdkAnalyzerDirectories
    )
    {
        string? referencePackVersion = TryGetReferencePackVersion(path);
        string? simpleAssemblyName = new AssemblyName(assemblyIdentity).Name;
        AnalyzerDisposition disposition =
            isSourceGenerator
            && referencePackVersion is not null
            && IsKnownSdkGenerator(Path.GetFileName(path), simpleAssemblyName)
            && IsAdmittedDirectory(path, admittedSdkAnalyzerDirectories)
                ? AnalyzerDisposition.SdkOwnedSourceGeneratorPendingProbe
                : AnalyzerDisposition.Ordinary;
        return new AnalyzerMetadataInspection(
            path,
            isSourceGenerator,
            false,
            assemblyIdentity,
            sha256,
            disposition,
            referencePackVersion
        );
    }

    private static bool IsAdmittedDirectory(string path, IReadOnlyCollection<string>? admittedDirectories)
    {
        if (admittedDirectories is null)
            return false;
        string directory = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(path)!);
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return admittedDirectories.Any(candidate =>
            directory.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate)), comparison)
        );
    }

    private static string? TryGetReferencePackVersion(string path)
    {
        DirectoryInfo? directory = new(Path.GetDirectoryName(path)!);
        if (!string.Equals(directory.Name, "cs", StringComparison.OrdinalIgnoreCase))
            return null;
        directory = directory.Parent;
        if (directory is null || !string.Equals(directory.Name, "dotnet", StringComparison.OrdinalIgnoreCase))
            return null;
        directory = directory.Parent;
        if (directory is null || !string.Equals(directory.Name, "analyzers", StringComparison.OrdinalIgnoreCase))
            return null;
        directory = directory.Parent;
        if (
            directory is null
            || !string.Equals(directory.Parent?.Name, "Microsoft.NETCore.App.Ref", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(directory.Parent?.Parent?.Name, "packs", StringComparison.OrdinalIgnoreCase)
        )
        {
            return null;
        }
        return directory.Name;
    }

    private static bool IsKnownSdkGenerator(string fileName, string? simpleAssemblyName)
    {
        string expectedName = Path.GetFileNameWithoutExtension(fileName);
        if (!string.Equals(expectedName, simpleAssemblyName, StringComparison.Ordinal))
            return false;
        return fileName switch
        {
            "Microsoft.Interop.ComInterfaceGenerator.dll" => true,
            "Microsoft.Interop.JavaScript.JSImportGenerator.dll" => true,
            "Microsoft.Interop.LibraryImportGenerator.dll" => true,
            "System.Text.Json.SourceGeneration.dll" => true,
            "System.Text.RegularExpressions.Generator.dll" => true,
            _ => false,
        };
    }

    private static string? TryGetTypeName(MetadataReader metadata, EntityHandle handle)
    {
        return handle.Kind switch
        {
            HandleKind.TypeReference => GetTypeReferenceName(
                metadata,
                metadata.GetTypeReference((TypeReferenceHandle)handle)
            ),
            HandleKind.TypeDefinition => GetTypeDefinitionName(
                metadata,
                metadata.GetTypeDefinition((TypeDefinitionHandle)handle)
            ),
            HandleKind.TypeSpecification => null,
            _ => throw new BadImageFormatException("Analyzer interface metadata is not a type."),
        };
    }

    private static string? TryGetAttributeTypeName(MetadataReader metadata, CustomAttribute attribute)
    {
        EntityHandle declaringType = attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => metadata
                .GetMemberReference((MemberReferenceHandle)attribute.Constructor)
                .Parent,
            HandleKind.MethodDefinition => metadata
                .GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor)
                .GetDeclaringType(),
            _ => throw new BadImageFormatException("Analyzer custom-attribute constructor is invalid."),
        };
        return TryGetTypeName(metadata, declaringType);
    }

    private static string GetTypeReferenceName(MetadataReader metadata, TypeReference reference) =>
        GetNamespace(metadata, reference.Namespace) + metadata.GetString(reference.Name);

    private static string GetTypeDefinitionName(MetadataReader metadata, TypeDefinition definition) =>
        GetNamespace(metadata, definition.Namespace) + metadata.GetString(definition.Name);

    private static string GetNamespace(MetadataReader metadata, StringHandle handle)
    {
        string value = metadata.GetString(handle);
        return string.IsNullOrEmpty(value) ? string.Empty : value + ".";
    }
}
