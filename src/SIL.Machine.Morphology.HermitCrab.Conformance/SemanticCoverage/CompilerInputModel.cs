#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed class CompilerInputException : Exception
{
    internal CompilerInputException(string code, string message, Exception? inner = null) : base(message, inner) => Code = code;

    internal string Code { get; }
}

internal sealed record CompilerToolchainIdentity(string CompilerDirectory);

internal enum AnalyzerDisposition
{
    Ordinary,
    SdkOwnedSourceGeneratorPendingProbe,
}

internal sealed record CompilerInputModel(
    CommandLineArguments Arguments,
    IReadOnlyList<string> Symbols,
    IReadOnlyList<CompilerSourceClassification> Sources,
    IReadOnlyList<AnalyzerMetadataInspection> Analyzers,
    IReadOnlyList<CompilerAuxiliaryInput> AdditionalFiles,
    IReadOnlyList<CompilerAuxiliaryInput> AnalyzerConfigs);

internal sealed record CompilerAuxiliaryInput(string Path, ImmutableArray<byte> Content);

internal enum CompilerSourceKind
{
    Owned,
    GeneratedSupport,
}

internal sealed record CompilerSourceClassification(
    string Path,
    CompilerSourceKind Kind,
    ImmutableArray<byte> Content);
