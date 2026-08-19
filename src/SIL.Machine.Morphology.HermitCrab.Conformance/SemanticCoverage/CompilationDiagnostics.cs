#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed record CompilationDiagnostic(
    string Origin,
    DiagnosticSeverity Severity,
    bool IsWarningAsError,
    string Code,
    string Message,
    string Location)
{
    internal bool IsFatal => Severity == DiagnosticSeverity.Error || IsWarningAsError;

    internal static CompilationDiagnostic From(string origin, Diagnostic diagnostic)
    {
        FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
        string location = string.IsNullOrEmpty(span.Path)
            ? string.Empty
            : $"{span.Path}:{span.StartLinePosition.Line + 1}:{span.StartLinePosition.Character + 1}";
        return new CompilationDiagnostic(
            origin,
            diagnostic.Severity,
            diagnostic.IsWarningAsError,
            diagnostic.Id,
            diagnostic.GetMessage(),
            location);
    }
}

internal sealed class CompilationDiagnostics
{
    private CompilationDiagnostics(IReadOnlyList<CompilationDiagnostic> all)
    {
        All = new ReadOnlyCollection<CompilationDiagnostic>(all.ToArray());
        Errors = new ReadOnlyCollection<CompilationDiagnostic>(all.Where(item => item.IsFatal).ToArray());
        Warnings = new ReadOnlyCollection<CompilationDiagnostic>(all
            .Where(item => item.Severity == DiagnosticSeverity.Warning && !item.IsFatal)
            .ToArray());
    }

    internal IReadOnlyList<CompilationDiagnostic> All { get; }
    internal IReadOnlyList<CompilationDiagnostic> Errors { get; }
    internal IReadOnlyList<CompilationDiagnostic> Warnings { get; }

    internal static CompilationDiagnostics From(string origin, IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        ArgumentNullException.ThrowIfNull(diagnostics);
        return new CompilationDiagnostics(diagnostics
            .Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Hidden && diagnostic.Severity != DiagnosticSeverity.Info)
            .Select(diagnostic => CompilationDiagnostic.From(origin, diagnostic))
            .ToArray());
    }

    internal void ThrowIfFatal()
    {
        if (Errors.Count == 0)
            return;
        throw new CompilerInputException(
            "compiler-error",
            $"Compilation contains {Errors.Count} error(s): {string.Join("; ", Errors.Take(10).Select(error => $"{error.Code} at {error.Location}: {error.Message}"))}");
    }
}
