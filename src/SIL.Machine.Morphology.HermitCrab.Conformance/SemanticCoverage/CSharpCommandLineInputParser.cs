#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal static class CSharpCommandLineInputParser
{
    internal static CompilerInputModel Parse(
        CapturedCompilerInputs captured,
        string repositoryRoot,
        string projectDirectory,
        string intermediateDirectory,
        IReadOnlyList<string>? profileSymbols = null,
        CompilerToolchainIdentity? queriedToolchain = null)
    {
        ArgumentNullException.ThrowIfNull(captured);
        if (captured.Items.TryGetValue("Using", out IReadOnlyList<CapturedCompilerItem>? usingItems) && usingItems.Count != 0)
            throw new CompilerInputException("unsupported-implicit-global-using", "Implicit global using inputs are not supported.");
        CompilerInputModel model = Parse(
            captured.Items["CscCommandLineArgs"].Select(item => item.Identity).ToArray(),
            projectDirectory,
            profileSymbols,
            queriedToolchain,
            AdmittedSdkAnalyzerDirectories(captured.Items["ReferencePathWithRefAssemblies"]));
        var classifier = new CompilerSourceClassifier(
            repositoryRoot,
            projectDirectory,
            intermediateDirectory,
            captured.Properties["GeneratedAssemblyInfoFile"],
            captured.Properties["TargetFrameworkMonikerAssemblyAttributesPath"]);
        var compileItems = captured.Items["Compile"];
        StringComparer pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var byIdentity = new Dictionary<string, CapturedCompilerItem>(pathComparer);
        foreach (CapturedCompilerItem item in compileItems)
        {
            string itemPath = item.Metadata.TryGetValue("FullPath", out string? fullPath)
                ? fullPath
                : item.Identity;
            itemPath = Path.GetFullPath(itemPath, projectDirectory);
            if (!byIdentity.TryAdd(itemPath, item))
                throw new CompilerInputException("duplicate-compiler-source", $"Compiler source '{item.Identity}' occurs more than once.");
        }
        var sources = new List<CompilerSourceClassification>();
        var parsedSourcePaths = new HashSet<string>(pathComparer);
        for (int sourceIndex = 0; sourceIndex < model.Arguments.SourceFiles.Length; sourceIndex++)
        {
            string source = model.Arguments.SourceFiles[sourceIndex].Path;
            string sourcePath = Path.GetFullPath(source, projectDirectory);
            if (!parsedSourcePaths.Add(sourcePath))
                throw new CompilerInputException("duplicate-compiler-source", $"Compiler source '{source}' occurs more than once in compiler arguments.");
            if (!byIdentity.TryGetValue(sourcePath, out CapturedCompilerItem? item))
                throw new CompilerInputException("unsupported-compiler-source", $"Compiler source '{source}' has no captured Compile item.");
            sources.Add(new CompilerSourceClassification(
                sourcePath,
                classifier.Classify(sourcePath, item.Metadata),
                model.Sources[sourceIndex].Content));
        }
        if (sources.Count != compileItems.Count || !parsedSourcePaths.SetEquals(byIdentity.Keys))
            throw new CompilerInputException("unsupported-compiler-source", "Captured Compile items and parsed source files differ.");
        return model with { Sources = sources };
    }

    internal static CompilerInputModel Parse(
        IReadOnlyList<string> commandLine,
        string baseDirectory,
        IReadOnlyList<string>? profileSymbols = null,
        CompilerToolchainIdentity? queriedToolchain = null,
        IReadOnlyCollection<string>? admittedSdkAnalyzerDirectories = null)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        baseDirectory = Path.GetFullPath(baseDirectory);
        if (queriedToolchain is not null)
            VerifyToolchain(queriedToolchain);

        CommandLineArguments parsed;
        try
        {
            parsed = CSharpCommandLineParser.Default.Parse(commandLine, baseDirectory, null, null);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new CompilerInputException("compiler-parser-diagnostic", exception.Message, exception);
        }

        if (parsed.Errors.Length != 0)
            throw new CompilerInputException(ClassifyParserError(parsed.Errors), string.Join("; ", parsed.Errors.Select(error => error.ToString())));

        var symbols = new SortedSet<string>(StringComparer.Ordinal);
        AddSymbols(symbols, parsed.ParseOptions.PreprocessorSymbolNames);
        if (profileSymbols is not null)
        {
            string[] missingSymbols = profileSymbols.Where(symbol => !symbols.Contains(symbol)).ToArray();
            if (missingSymbols.Length != 0)
            {
                throw new CompilerInputException(
                    "compiler-profile-symbol-mismatch",
                    $"Compiler arguments are missing profile symbols: {string.Join(", ", missingSymbols)}.");
            }
        }

        var sources = parsed.SourceFiles
            .Select(source => new CompilerSourceClassification(
                source.Path,
                CompilerSourceKind.Owned,
                ReadSourceContent(source.Path)))
            .ToArray();
        var analyzers = parsed.AnalyzerReferences
            .Select(reference => AnalyzerMetadataInspector.Inspect(reference.FilePath, admittedSdkAnalyzerDirectories))
            .ToArray();
        string[] sourceGenerators = analyzers
            .Where(analyzer => analyzer.IsSourceGenerator &&
                analyzer.Disposition != AnalyzerDisposition.SdkOwnedSourceGeneratorPendingProbe)
            .Select(analyzer => analyzer.Path)
            .ToArray();
        if (sourceGenerators.Length != 0)
        {
            throw new CompilerInputException(
                "unsupported-source-generator",
                $"Source-generator analyzers are not supported: {string.Join(", ", sourceGenerators)}.");
        }

        CompilerAuxiliaryInput[] additionalFiles = parsed.AdditionalFiles
            .Select(file => ReadAuxiliaryInput(file.Path))
            .ToArray();
        CompilerAuxiliaryInput[] analyzerConfigs = parsed.AnalyzerConfigPaths
            .Select(ReadAuxiliaryInput)
            .ToArray();
        return new CompilerInputModel(parsed, symbols.ToArray(), sources, analyzers, additionalFiles, analyzerConfigs);
    }

    private static void VerifyToolchain(CompilerToolchainIdentity queried)
    {
        string csharpParserPath = typeof(CSharpCommandLineParser).Assembly.Location;
        string commonParserPath = typeof(CommandLineArguments).Assembly.Location;
        string directory = queried.CompilerDirectory;
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory) || !Directory.Exists(directory))
            throw new CompilerInputException("incompatible-compiler-toolchain", $"Queried compiler directory '{directory}' is not an existing absolute directory; parser is '{csharpParserPath}'.");
        VerifyCompilerAssembly(directory, "Microsoft.CodeAnalysis.CSharp.dll", csharpParserPath);
        VerifyCompilerAssembly(directory, "Microsoft.CodeAnalysis.dll", commonParserPath);
    }

    private static void VerifyCompilerAssembly(string directory, string fileName, string parserPath)
    {
        string queriedPath = Path.Combine(directory, fileName);
        if (!File.Exists(queriedPath) || !SameAssemblyContent(parserPath, queriedPath))
            throw new CompilerInputException("incompatible-compiler-toolchain", $"Queried compiler '{queriedPath}' does not match parser '{parserPath}'.");
    }

    private static bool SameAssemblyContent(string left, string right)
    {
        try
        {
            using FileStream leftStream = File.OpenRead(left);
            using FileStream rightStream = File.OpenRead(right);
            byte[] leftHash = SHA256.HashData(leftStream);
            byte[] rightHash = SHA256.HashData(rightStream);
            return leftHash.AsSpan().SequenceEqual(rightHash);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string ClassifyParserError(System.Collections.Immutable.ImmutableArray<Diagnostic> errors) =>
        errors.Any(error => error.Id is "CS0006" or "CS0009" ||
                error.GetMessage().Contains("reference", StringComparison.OrdinalIgnoreCase) ||
                error.GetMessage().Contains("metadata file", StringComparison.OrdinalIgnoreCase))
            ? "reference-parser-diagnostic"
            : errors.Any(error => error.GetMessage().Contains("unrecognized", StringComparison.OrdinalIgnoreCase) || error.GetMessage().Contains("unknown", StringComparison.OrdinalIgnoreCase))
                ? "unknown-compiler-switch"
                : "compiler-parser-diagnostic";

    private static void AddSymbols(ISet<string> destination, IEnumerable<string> symbols)
    {
        foreach (string symbol in symbols.Where(symbol => !string.IsNullOrWhiteSpace(symbol)))
            destination.Add(symbol);
    }

    private static ImmutableArray<byte> ReadSourceContent(string path)
    {
        try
        {
            return File.ReadAllBytes(path).ToImmutableArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new CompilerInputException("compiler-source-read-diagnostic", $"Cannot read compiler source '{path}'.", exception);
        }
    }

    private static CompilerAuxiliaryInput ReadAuxiliaryInput(string path)
    {
        try
        {
            string fullPath = Path.GetFullPath(path);
            return new CompilerAuxiliaryInput(fullPath, File.ReadAllBytes(fullPath).ToImmutableArray());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new CompilerInputException("compiler-auxiliary-read-diagnostic", $"Cannot read compiler auxiliary input '{path}'.", exception);
        }
    }

    private static IReadOnlyCollection<string> AdmittedSdkAnalyzerDirectories(
        IReadOnlyList<CapturedCompilerItem> references)
    {
        var directories = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (CapturedCompilerItem reference in references)
        {
            string path = reference.Metadata.TryGetValue("FullPath", out string? fullPath) ? fullPath : reference.Identity;
            string canonical = Path.GetFullPath(path);
            string marker = $"{Path.DirectorySeparatorChar}packs{Path.DirectorySeparatorChar}Microsoft.NETCore.App.Ref{Path.DirectorySeparatorChar}";
            int markerIndex = canonical.IndexOf(marker, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            if (markerIndex < 0)
                continue;
            string packRoot = canonical[..(markerIndex + marker.Length)];
            string remainder = canonical[(markerIndex + marker.Length)..];
            string version = remainder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            if (!string.IsNullOrWhiteSpace(version))
                directories.Add(Path.Combine(packRoot, version, "analyzers", "dotnet", "cs"));
        }
        return directories;
    }
}
