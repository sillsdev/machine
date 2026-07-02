using System.Linq;
using ManyConsole;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Thin CLI wrapper around <see cref="GrammarAnalyzer.Analyze"/> (complexity-cap.md §6.3) — lets
/// machine.py users and CI-style grammar validation run the static lint outside FLEx.
/// </summary>
internal class LintCommand : ConsoleCommand
{
    private readonly HCContext _context;
    private string _severity;

    public LintCommand(HCContext context)
    {
        _context = context;

        IsCommand("lint", "Runs static grammar analysis and reports diagnostics (see complexity-cap.md).");
        SkipsCommandSummaryBeforeRunning();
        HasOption(
            "s|severity=",
            "minimum severity to report: info, warning, or error (default: info)",
            o => _severity = o
        );
    }

    public override int Run(string[] remainingArguments)
    {
        DiagnosticSeverity minSeverity = ParseSeverity(_severity);
        var diagnostics = GrammarAnalyzer
            .Analyze(_context.Language)
            .Where(d => d.Severity >= minSeverity)
            .OrderBy(d => d.Code)
            .ToList();

        if (diagnostics.Count == 0)
        {
            _context.Out.WriteLine("No grammar diagnostics found.");
        }
        else
        {
            foreach (GrammarDiagnostic diagnostic in diagnostics)
            {
                _context.Out.WriteLine("{0} [{1}] {2}", diagnostic.Code, diagnostic.Severity, diagnostic.Message);
                _context.Out.WriteLine("    Suggestion: {0}", diagnostic.Suggestion);
            }
            _context.Out.WriteLine();
            _context.Out.WriteLine("{0} diagnostic(s).", diagnostics.Count);
        }

        _context.Out.WriteLine();
        return 0;
    }

    private static DiagnosticSeverity ParseSeverity(string severity)
    {
        switch (severity?.ToLowerInvariant())
        {
            case "warning":
                return DiagnosticSeverity.Warning;
            case "error":
                return DiagnosticSeverity.Error;
            default:
                return DiagnosticSeverity.Info;
        }
    }
}
