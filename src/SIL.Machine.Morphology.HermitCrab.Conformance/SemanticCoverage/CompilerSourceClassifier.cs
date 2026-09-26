#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed class CompilerSourceClassifier
{
    private readonly string _repositoryRoot;
    private readonly string _projectDirectory;
    private readonly string _intermediateDirectory;
    private readonly string _assemblyInfoPath;
    private readonly string _targetFrameworkAttributesPath;

    internal CompilerSourceClassifier(
        string repositoryRoot,
        string projectDirectory,
        string intermediateDirectory,
        string assemblyInfoPath,
        string targetFrameworkAttributesPath
    )
    {
        _repositoryRoot = Canonical(repositoryRoot);
        _projectDirectory = Canonical(projectDirectory);
        _intermediateDirectory = Canonical(intermediateDirectory);
        _assemblyInfoPath = Canonical(assemblyInfoPath);
        _targetFrameworkAttributesPath = Canonical(targetFrameworkAttributesPath);
        if (
            !IsWithin(_assemblyInfoPath, _intermediateDirectory)
            || !IsWithin(_targetFrameworkAttributesPath, _intermediateDirectory)
        )
        {
            throw new CompilerInputException(
                "unsupported-compiler-source",
                "MSBuild generated-source paths must remain within the private intermediate directory."
            );
        }
    }

    internal CompilerSourceKind Classify(string path, IReadOnlyDictionary<string, string>? metadata = null)
    {
        string canonical = Canonical(path);
        if (IsWithin(canonical, _intermediateDirectory))
        {
            if (IsAdmittedGeneratedPath(canonical))
                return CompilerSourceKind.GeneratedSupport;
            throw new CompilerInputException(
                "unsupported-compiler-source",
                $"Generated source '{path}' is not an admitted SDK support input."
            );
        }

        if (Path.GetFileName(canonical).Equals("GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase))
        {
            throw new CompilerInputException(
                "unsupported-implicit-global-using",
                "Implicit global using source is not supported."
            );
        }

        if (
            IsWithin(canonical, _projectDirectory)
            && IsWithin(canonical, _repositoryRoot)
            && !HasGeneratedMetadata(metadata)
            && !ContainsBuildDirectory(canonical)
        )
        {
            return CompilerSourceKind.Owned;
        }

        throw new CompilerInputException(
            "unsupported-compiler-source",
            $"Compiler source '{path}' is outside the admitted source set."
        );
    }

    private bool IsAdmittedGeneratedPath(string path)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return path.Equals(_assemblyInfoPath, comparison) || path.Equals(_targetFrameworkAttributesPath, comparison);
    }

    private static bool HasGeneratedMetadata(IReadOnlyDictionary<string, string>? metadata) =>
        metadata is not null
        && (
            (
                metadata.TryGetValue("Generated", out string? generated)
                && generated.Equals("true", StringComparison.OrdinalIgnoreCase)
            )
            || (
                metadata.TryGetValue("AutoGen", out string? autoGen)
                && autoGen.Equals("true", StringComparison.OrdinalIgnoreCase)
            )
        );

    private bool ContainsBuildDirectory(string path)
    {
        string relative = Path.GetRelativePath(_projectDirectory, path);
        return relative
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment =>
                segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static string Canonical(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static bool IsWithin(string path, string directory)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return path.Equals(directory, comparison)
            || path.StartsWith(directory + Path.DirectorySeparatorChar, comparison);
    }
}
